using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BallThrower : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform ballSpawnPoint;
    public Transform hoopRingTarget;   // <-- your Ring object (hoop center)
    public LineRenderer line;

    [Header("Hold Position (relative to spawn point)")]
    public Vector3 holdLocalOffset = Vector3.zero;

    [Header("Throw Tuning")]
    public float minPower = 10f;          // minimum throw strength
    public float maxPower = 18f;          // maximum throw strength
    public float upwardBoost = 0.45f;     // base arc
    public float arcFromSwipe = 0.35f;    // extra arc from swipe up/down
    public float maxDragPixels = 350f;
    public float minSwipePixels = 30f;

    [Header("Trajectory Preview")]
    public int linePoints = 25;
    public float timeStep = 0.06f;
    public float lineStartOffset = 0.05f;

    [Header("Miss Rules")]
    public LayerMask groundMask;
    public float missBelowY = -2f;

    private Rigidbody rb;

    private bool isDragging = false;
    private Vector2 dragStart;
    private Vector2 dragCurrent;

    private bool inFlight = false;
    private bool passedTop = false;
    private bool scoredThisShot = false;
    private bool missRegistered = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (cam == null) cam = Camera.main;
        if (line == null) line = GetComponent<LineRenderer>();
        if (line != null) line.enabled = false;
    }

    private void Start()
    {
        ResetBall();
    }

    private void Update()
    {
        if (GameManager.I != null && GameManager.I.IsGameOver())
        {
            if (line != null) line.enabled = false;
            return;
        }

        HandleInput_NewInputSystem();

        if (inFlight && !missRegistered && transform.position.y < missBelowY)
        {
            RegisterMissAndReset();
        }
    }

    // -------------------- NEW INPUT SYSTEM --------------------
    private void HandleInput_NewInputSystem()
    {
        if (inFlight) return;

        var ts = Touchscreen.current;
        if (ts != null)
        {
            bool pressed = ts.primaryTouch.press.isPressed;

            if (pressed)
            {
                Vector2 pos = ts.primaryTouch.position.ReadValue();
                if (!isDragging) StartDrag(pos);
                else ContinueDrag(pos);
                return;
            }
            else if (isDragging)
            {
                EndDrag(dragCurrent);
                return;
            }
        }

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
            StartDrag(mouse.position.ReadValue());
        else if (mouse.leftButton.isPressed)
            ContinueDrag(mouse.position.ReadValue());
        else if (mouse.leftButton.wasReleasedThisFrame)
            EndDrag(mouse.position.ReadValue());
    }

    // -------------------- DRAG --------------------
    private void StartDrag(Vector2 screenPos)
    {
        isDragging = true;
        dragStart = screenPos;
        dragCurrent = screenPos;

        if (line != null) line.enabled = true;

        HoldBallAtSpawn();
    }

    private void ContinueDrag(Vector2 screenPos)
    {
        if (!isDragging) return;

        dragCurrent = screenPos;
        HoldBallAtSpawn();
        UpdateTrajectoryPreview();
    }

    private void EndDrag(Vector2 screenPos)
    {
        if (!isDragging) return;

        isDragging = false;
        dragCurrent = screenPos;

        if (line != null) line.enabled = false;

        Vector2 swipe = dragCurrent - dragStart;
        if (swipe.magnitude < minSwipePixels)
        {
            HoldBallAtSpawn();
            return;
        }

        ThrowTowardRing(swipe);
    }

    // -------------------- THROW (ALWAYS TOWARD RING) --------------------
    private void ThrowTowardRing(Vector2 swipe)
    {
        if (hoopRingTarget == null)
        {
            Debug.LogError("BallThrower: hoopRingTarget (Ring) not assigned!");
            return;
        }

        rb.isKinematic = false;

        float swipeStrength01 = Mathf.Clamp01(swipe.magnitude / maxDragPixels);
        float vertical01 = Mathf.Clamp(swipe.y / maxDragPixels, -1f, 1f);

        float power = Mathf.Lerp(minPower, maxPower, swipeStrength01);

        // Base direction: from ball to ring center
        Vector3 toRing = (hoopRingTarget.position - transform.position).normalized;

        // Add arc: always some upward + swipe-controlled arc
        Vector3 dir = toRing + Vector3.up * (upwardBoost + (vertical01 * arcFromSwipe));
        dir = dir.normalized;

        rb.AddForce(dir * power, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.VelocityChange);

        inFlight = true;
        passedTop = false;
        scoredThisShot = false;
        missRegistered = false;
    }

    // -------------------- HOLD / RESET --------------------
    private void HoldBallAtSpawn()
    {
        if (ballSpawnPoint == null)
        {
            Debug.LogError("BallThrower: ballSpawnPoint is not assigned!");
            return;
        }

        // Unity 6 fix: clear velocity while dynamic, then set kinematic
        if (rb.isKinematic) rb.isKinematic = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep();

        rb.isKinematic = true;

        transform.position = ballSpawnPoint.TransformPoint(holdLocalOffset);
        transform.rotation = ballSpawnPoint.rotation;
    }

    private void ResetBall()
    {
        inFlight = false;
        passedTop = false;
        scoredThisShot = false;
        missRegistered = false;

        HoldBallAtSpawn();
    }

    // -------------------- SCORING GATE --------------------
    public void MarkPassedTop()
    {
        if (!inFlight) return;
        passedTop = true;
    }

    public void TryScoreFromBottom()
    {
        if (!inFlight) return;
        if (scoredThisShot) return;

        if (passedTop)
        {
            scoredThisShot = true;

            if (GameManager.I != null)
                GameManager.I.AddScore(1);

            Invoke(nameof(ResetBall), 0.35f);
        }
    }

    // -------------------- MISS --------------------
    private void OnCollisionEnter(Collision collision)
    {
        if (!inFlight || missRegistered) return;

        bool hitGround = ((1 << collision.gameObject.layer) & groundMask) != 0;
        if (hitGround && !scoredThisShot)
        {
            RegisterMissAndReset();
        }
    }

    private void RegisterMissAndReset()
    {
        missRegistered = true;

        if (GameManager.I != null)
            GameManager.I.RegisterMiss();

        if (GameManager.I != null && !GameManager.I.IsGameOver())
            Invoke(nameof(ResetBall), 0.25f);
    }

    // -------------------- TRAJECTORY PREVIEW --------------------
    private void UpdateTrajectoryPreview()
    {
        if (line == null || hoopRingTarget == null) return;

        Vector2 swipe = dragCurrent - dragStart;
        if (swipe.magnitude < minSwipePixels)
        {
            line.positionCount = 0;
            return;
        }

        float swipeStrength01 = Mathf.Clamp01(swipe.magnitude / maxDragPixels);
        float vertical01 = Mathf.Clamp(swipe.y / maxDragPixels, -1f, 1f);

        float power = Mathf.Lerp(minPower, maxPower, swipeStrength01);

        Vector3 toRing = (hoopRingTarget.position - transform.position).normalized;
        Vector3 dir = toRing + Vector3.up * (upwardBoost + (vertical01 * arcFromSwipe));
        dir = dir.normalized;

        Vector3 startVelocity = dir * power;
        Vector3 startPos = transform.position + dir * lineStartOffset;

        DrawTrajectory(startPos, startVelocity);
    }

    private void DrawTrajectory(Vector3 startPos, Vector3 startVelocity)
    {
        line.positionCount = linePoints;

        Vector3 gravity = Physics.gravity;
        for (int i = 0; i < linePoints; i++)
        {
            float t = i * timeStep;
            Vector3 point = startPos + startVelocity * t + 0.5f * gravity * t * t;
            line.SetPosition(i, point);
        }
    }
}
