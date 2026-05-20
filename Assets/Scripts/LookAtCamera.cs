using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private bool isTopDownMode = true;

    void Update()
    {
        Camera activeCamera = Camera.main;

        isTopDownMode = activeCamera != null && activeCamera.orthographic;

        if (activeCamera != null)
        {
            if (isTopDownMode)
            {
                transform.eulerAngles = new Vector3(90f, 0f, 0f);
            }
            else
            {
                // 3D mode: full look at
                transform.LookAt(activeCamera.transform);
            }
        }
    }
}