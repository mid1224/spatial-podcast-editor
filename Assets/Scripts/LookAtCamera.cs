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
                Vector3 forward = transform.position - activeCamera.transform.position;
                if (forward.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                }
            }
        }
    }
}