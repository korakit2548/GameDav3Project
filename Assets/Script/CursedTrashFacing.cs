using UnityEngine;

public static class CursedTrashFacing
{
    public static void FaceDoor(GameObject cursedTrashObject, Transform roomDoor)
    {
        if (cursedTrashObject == null || roomDoor == null)
        {
            return;
        }

        Transform bearVisual = cursedTrashObject.transform.Find("Doll_CursedObjects");
        if (bearVisual == null)
        {
            return;
        }

        Vector3 toDoor = Vector3.ProjectOnPlane(roomDoor.position - bearVisual.position, Vector3.up);
        // The imported doll's EYE submesh sits in front of its head along local +Z.
        Vector3 currentFacing = Vector3.ProjectOnPlane(bearVisual.forward, Vector3.up);
        if (toDoor.sqrMagnitude < 0.0001f || currentFacing.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float yaw = Vector3.SignedAngle(currentFacing, toDoor, Vector3.up);
        if (roomDoor.gameObject.scene.name == "Tutorial") yaw += 180f;
        bearVisual.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * bearVisual.rotation;
    }
}
