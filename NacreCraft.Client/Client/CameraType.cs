namespace NacreCraft.Client;

//CameraType third-person camera perspective, maps to vanilla net.minecraft.client.CameraType
public enum CameraType
{
    FirstPerson,
    ThirdPersonBack,
    ThirdPersonFront
}

public static class CameraTypeExtensions
{
    //IsFirstPerson whether the camera sits in the player's eyes
    public static bool IsFirstPerson(this CameraType type) => type == CameraType.FirstPerson;

    //IsMirrored whether the third-person view is mirrored (front view)
    public static bool IsMirrored(this CameraType type) => type == CameraType.ThirdPersonFront;

    //Cycle advances to the next perspective
    public static CameraType Cycle(this CameraType type)
        => type switch
        {
            CameraType.FirstPerson => CameraType.ThirdPersonBack,
            CameraType.ThirdPersonBack => CameraType.ThirdPersonFront,
            _ => CameraType.FirstPerson
        };
}
