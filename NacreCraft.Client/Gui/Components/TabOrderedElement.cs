namespace NacreCraft.Client.Gui.Components;

//TabOrderedElement widgets grouped for tab navigation, maps to vanilla TabOrderedElement
public interface TabOrderedElement
{
    //GetTabOrderGroup groups widgets so tabbing stays inside a group before moving on
    int GetTabOrderGroup() => 0;
}
