using UnityEngine;

public class GrapplingGun : MonoBehaviour
{
    private LineRenderer lr;
    private Vector3 grapplePoint;
    public LayerMask whatIsGrappleable;
    public Transform gunTip, camera, player;
    private float maxDistance = 15f;
    private SpringJoint joint;

    [Header("Aim assist")]
    public bool useAimAssist = true;
    public float sphereCastRadius = 3f; // прощает промах прицела на этот радиус

    [Header("Rope visuals")]
    public int ropeSegments = 16;         // сегментов для изгиба во время полёта
    public float ropeExtendSpeed = 20f;   // скорость "вылета" верёвки, юнитов в секунду
    public float whipCurveAmount = 2f;  // сила изгиба "хлыста" во время полёта
    public float whipFrequency = 1.5f;    // сколько "волн" укладывается по длине верёвки

    // Состояние анимации верёвки
    private float extendProgress;   // 0..1, прогресс "вылета" верёвки к точке
    private bool isExtending;
    private Vector3[] ropePositions;
    private Vector3 flightDirection; // направление полёта крюка (фиксируется в момент выстрела)

    void Awake() {
        lr = GetComponent<LineRenderer>();
    }

    void Update() {
        if (Input.GetMouseButtonDown(1)) {
            StartGrapple();
        }
        else if (Input.GetMouseButtonUp(1)) {
            StopGrapple();
        }

        if (isExtending) {
            float dist = Vector3.Distance(gunTip.position, grapplePoint);
            extendProgress += Time.deltaTime * ropeExtendSpeed / Mathf.Max(dist, 0.01f);

            if (extendProgress >= 1f) {
                extendProgress = 1f;
                isExtending = false;

                // Только теперь, когда верёвка реально долетела до точки,
                // включаем физику крюка.
                AttachJoint();
            }
        }
    }

    //Called after Update
    void LateUpdate() {
        DrawRope();
    }

    /// <summary>
    /// Call whenever we want to start a grapple
    /// </summary>
    void StartGrapple() {
        if (!TryFindGrapplePoint(out grapplePoint)) return;

        extendProgress = 0f;
        isExtending = true;
        lr.positionCount = ropeSegments;

        flightDirection = (grapplePoint - gunTip.position).normalized;
    }

    void AttachJoint() {
        joint = player.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = grapplePoint;

        float distanceFromPoint = Vector3.Distance(player.position, grapplePoint);

        //The distance grapple will try to keep from grapple point. 
        joint.maxDistance = distanceFromPoint * 0.8f;
        joint.minDistance = distanceFromPoint * 0.25f;

        //Adjust these values to fit your game.
        joint.spring = 4.5f;
        joint.damper = 7f;
        joint.massScale = 4.5f;
    }

    /// <summary>
    /// Ищет точку крепления двумя способами:
    /// 1) точный Raycast от камеры (обычное прицеливание);
    /// 2) SphereCast — прощает промах прицела на sphereCastRadius,
    ///    т.е. можно зацепиться, даже если прицел чуть в стороне от нужного слоя.
    /// </summary>
    bool TryFindGrapplePoint(out Vector3 point) {
        RaycastHit hit;

        // 1) Точный прицел
        if (Physics.Raycast(camera.position, camera.forward, out hit, maxDistance, whatIsGrappleable)) {
            point = hit.point;
            return true;
        }

        // 2) Прощающий SphereCast вдоль направления взгляда
        if (useAimAssist && Physics.SphereCast(camera.position, sphereCastRadius, camera.forward, out hit, maxDistance, whatIsGrappleable)) {
            point = hit.point;
            return true;
        }

        point = Vector3.zero;
        return false;
    }


    /// <summary>
    /// Call whenever we want to stop a grapple
    /// </summary>
    void StopGrapple() {
        lr.positionCount = 0;
        isExtending = false;
        if (joint != null) Destroy(joint);
    }

    void DrawRope() {
        // Если крюк вообще не активен (ни летит, ни зацеплен) — не рисуем верёвку
        if (!isExtending && joint == null) return;

        // Когда крюк уже зацепился — верёвка прямая, без изгиба
        if (!isExtending) {
            lr.positionCount = 2;
            lr.SetPosition(0, gunTip.position);
            lr.SetPosition(1, grapplePoint);
            return;
        }

        if (lr.positionCount != ropeSegments) lr.positionCount = ropeSegments;
        if (ropePositions == null || ropePositions.Length != ropeSegments)
            ropePositions = new Vector3[ropeSegments];

        Vector3 start = gunTip.position;

        // easeOut — кончик верёвки летит быстро и слегка "докручивает" перед точкой
        float easedT = 1f - Mathf.Pow(1f - extendProgress, 3f);
        Vector3 tip = Vector3.Lerp(start, grapplePoint, easedT);

        // Перпендикуляр к направлению полёта — вдоль него будем изгибать "хлыст"
        Vector3 perp = Vector3.Cross(flightDirection, Vector3.up);
        if (perp.sqrMagnitude < 0.001f) perp = Vector3.Cross(flightDirection, Vector3.right);
        perp.Normalize();

        // Изгиб сильнее всего в середине полёта (t ~ 0.5) и пропадает к 0 и к 1,
        // это и даёт ощущение "хлыста", а не прямой линии
        float curveStrength = Mathf.Sin(extendProgress * Mathf.PI) * whipCurveAmount;

        for (int i = 0; i < ropeSegments; i++) {
            float segmentT = (float)i / (ropeSegments - 1);

            // Точка вдоль пока ещё не долетевшей верёвки — от старта до текущего кончика
            Vector3 pointOnLine = Vector3.Lerp(start, tip, segmentT);

            // Бегущая волна вдоль верёвки: у основания и у кончика — почти без изгиба,
            // максимум где-то посередине, и вся волна "уезжает" вперёд по мере полёта
            float wave = Mathf.Sin(segmentT * Mathf.PI * whipFrequency - extendProgress * Mathf.PI * 2f)
                         * Mathf.Sin(segmentT * Mathf.PI); // зануляем на концах

            pointOnLine += perp * wave * curveStrength;

            ropePositions[i] = pointOnLine;
        }

        lr.SetPositions(ropePositions);
    }

    public bool IsGrappling() {
        return joint != null;
    }

    public Vector3 GetGrapplePoint() {
        return grapplePoint;
    }
}
