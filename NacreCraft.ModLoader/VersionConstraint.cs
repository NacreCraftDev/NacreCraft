namespace NacreCraft.ModLoader;

//VersionConstraint: version constraint evaluation for mod dependencies
//Constraint syntax
//  *        any version, equivalent to omitting the dependency
//  1.2.3    segment prefix match; 1.2 matches both 1.2.3 and 1.2
//  =1.2.3   exact match on every segment after zero padding, so =1.2 also matches 1.2.0
//  !=1.2.3  the negation of =
//  ^1.2.3   same major and not below the baseline; when the major is 0 it compares minor instead, so 0.1 and 0.2 are incompatible
//  >=1.2.3 / <= / > / <   not below / not above / above / below the baseline
//  a,b      both must hold; a||b    either must hold (|| splits first, then commas)
//Version numbers take only the leading digits of each segment, so 26.2-nacrecraft compares as 26.2
internal static class VersionConstraint
{
    //Matches: whether the version satisfies the constraint; empty or * always passes
    public static bool Matches(string? constraint, string? version)
    {
        if (string.IsNullOrWhiteSpace(constraint))
            return true;

        var text = constraint.Trim();
        if (text == "*")
            return true;

        //|| separates alternatives; an empty alternative never matches, and a constraint of only empties passes
        var alternatives = text.Split("||", StringSplitOptions.TrimEntries);
        var any = false;
        foreach (var alternative in alternatives)
        {
            if (alternative.Length == 0)
                continue;
            any = true;
            if (MatchesAnd(alternative, version))
                return true;
        }
        return !any;
    }

    //MatchesAnd: comma-separated terms that must all hold
    private static bool MatchesAnd(string text, string? version)
    {
        foreach (var term in text.Split(',', StringSplitOptions.TrimEntries))
        {
            if (term.Length == 0)
                continue;
            if (!MatchesTerm(term, version))
                return false;
        }
        return true;
    }

    //MatchesTerm: one predicate; the two-character forms are tried before their one-character prefixes
    private static bool MatchesTerm(string term, string? version)
    {
        if (term == "*")
            return true;

        if (term.StartsWith(">=", StringComparison.Ordinal))
            return Compare(ParseParts(version), ParseParts(term[2..])) >= 0;
        if (term.StartsWith("<=", StringComparison.Ordinal))
            return Compare(ParseParts(version), ParseParts(term[2..])) <= 0;
        if (term.StartsWith("!=", StringComparison.Ordinal))
            return !ExactEquals(version, term[2..]);
        if (term.StartsWith('='))
            return ExactEquals(version, term[1..]);
        if (term.StartsWith('>'))
            return Compare(ParseParts(version), ParseParts(term[1..])) > 0;
        if (term.StartsWith('<'))
            return Compare(ParseParts(version), ParseParts(term[1..])) < 0;

        if (term.StartsWith('^'))
        {
            var baseline = ParseParts(term[1..]);
            if (Compare(ParseParts(version), baseline) < 0)
                return false;
            return Major(baseline) == 0
                ? Minor(ParseParts(version)) == Minor(baseline)
                : Major(ParseParts(version)) == Major(baseline);
        }

        //A bare version is a segment prefix match, so 1.2 matches 1.2 and 1.2.3 but not 1.3
        //A term with no digits matches nothing, the same rule v1 used
        var parts = ParseParts(term);
        if (parts.Length == 0)
            return false;
        var actual = ParseParts(version);
        if (parts.Length > actual.Length)
            return false;
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i] != actual[i])
                return false;
        }
        return true;
    }

    //ExactEquals: every segment equal after zero padding, so =1.2 matches 1.2.0 but not 1.2.1
    private static bool ExactEquals(string? version, string baseline)
    {
        var left = ParseParts(version);
        var right = ParseParts(baseline);
        if (left.Length == 0 && right.Length == 0)
            return true;

        var length = Math.Max(left.Length, right.Length);
        for (var i = 0; i < length; i++)
        {
            var a = i < left.Length ? left[i] : 0;
            var b = i < right.Length ? right[i] : 0;
            if (a != b)
                return false;
        }
        return true;
    }

    //Major: the major segment, counted as 0 if absent
    private static int Major(int[] parts) => parts.Length > 0 ? parts[0] : 0;

    //Minor: the minor segment, counted as 0 if absent
    private static int Minor(int[] parts) => parts.Length > 1 ? parts[1] : 0;

    //Compare: compares segment by segment, missing segments are padded with 0, returns -1 / 0 / 1
    private static int Compare(int[] left, int[] right)
    {
        var length = Math.Max(left.Length, right.Length);
        for (var i = 0; i < length; i++)
        {
            var a = i < left.Length ? left[i] : 0;
            var b = i < right.Length ? right[i] : 0;
            if (a != b)
                return a < b ? -1 : 1;
        }
        return 0;
    }

    //ParseParts: the leading digits of each dot-separated segment; a segment without digits contributes nothing
    private static int[] ParseParts(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return [];

        var segments = version.Split('.');
        var parts = new List<int>(segments.Length);
        foreach (var segment in segments)
        {
            var digits = 0;
            while (digits < segment.Length && char.IsAsciiDigit(segment[digits]))
                digits++;
            if (digits == 0)
                continue;
            parts.Add(int.Parse(segment[..digits]));
        }
        return [.. parts];
    }
}
