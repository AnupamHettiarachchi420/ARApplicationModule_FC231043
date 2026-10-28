using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TapToPlace : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private GameObject prefab;
    [SerializeField] private float size = 0.2f; // side length in metres (0.2 = 20 cm)

    private static readonly List<ARRaycastHit> hits = new();
    private GameObject placed;

    private void Update()
    {
        var pointer = UnityEngine.InputSystem.Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;

        Vector2 pos = pointer.position.ReadValue();
        if (!raycastManager.Raycast(pos, hits, TrackableType.PlaneWithinPolygon)) return;

        Pose pose = hits[0].pose;
        Vector3 position = pose.position + pose.up * (size / 2f);

        if (placed == null)
        {
            placed = Instantiate(prefab, position, pose.rotation);
            placed.transform.localScale = Vector3.one * size;
        }
        else
        {
            placed.transform.SetPositionAndRotation(position, pose.rotation);
        }
    }
}