using Eldersoft.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private float maxDistance = 5;

    void Update()
    {
        RaycastHit hitInfo;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hitInfo, maxDistance, layerMask))
        {
            Debug.DrawLine(cam.transform.position, hitInfo.point, Color.cyan, .1f);
            Debug.Log("Hit: " + hitInfo.collider.name);

            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
            {
                ChatbotController.Instance.Open();
            }
        }
    }
}
