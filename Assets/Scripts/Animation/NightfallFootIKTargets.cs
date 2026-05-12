using UnityEngine;

public class NightfallFootIKTargets : MonoBehaviour
{
    [Header("Feet")]
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightFoot;
    [SerializeField] private Transform leftTarget;
    [SerializeField] private Transform rightTarget;
    [SerializeField] private Transform leftHint;
    [SerializeField] private Transform rightHint;

    [Header("Grounding")]
    [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;
    [SerializeField] private float raycastUp = 0.45f;
    [SerializeField] private float raycastDown = 1.2f;
    [SerializeField] private float footGroundOffset = 0.03f;
    [SerializeField] private float targetFollowSpeed = 28f;
    [SerializeField] private float hintForwardOffset = 0.35f;
    [SerializeField] private float hintUpOffset = 0.15f;
    [SerializeField] private bool alignFootToGroundNormal = true;

    private void LateUpdate()
    {
        UpdateFoot(leftFoot, leftTarget, leftHint);
        UpdateFoot(rightFoot, rightTarget, rightHint);
    }

    private void UpdateFoot(Transform foot, Transform target, Transform hint)
    {
        if (foot == null || target == null)
        {
            return;
        }

        Vector3 rayOrigin = foot.position + Vector3.up * raycastUp;
        float rayLength = raycastUp + raycastDown;
        Vector3 targetPosition = foot.position;
        Quaternion targetRotation = foot.rotation;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, groundMask, QueryTriggerInteraction.Ignore))
        {
            targetPosition = hit.point + Vector3.up * footGroundOffset;

            if (alignFootToGroundNormal)
            {
                targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * foot.rotation;
            }
        }

        float t = 1f - Mathf.Exp(-targetFollowSpeed * Time.deltaTime);
        target.position = Vector3.Lerp(target.position, targetPosition, t);
        target.rotation = Quaternion.Slerp(target.rotation, targetRotation, t);

        if (hint != null)
        {
            Vector3 hintPosition = foot.position + transform.forward * hintForwardOffset + Vector3.up * hintUpOffset;
            hint.position = Vector3.Lerp(hint.position, hintPosition, t);
        }
    }
}
