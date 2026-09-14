using UnityEngine;
using System.Collections;
public class Room : MonoBehaviour
{
    public enum RoomState
    {
        Normal,
        Warning,
        Cursed
    }


    [SerializeField]
    private RoomState currentState = RoomState.Normal;

    [SerializeField]
    private GameObject cursedTrashPrefab;

    [SerializeField]
    private Transform trashSpawnPoint;

    [SerializeField]
    private Light roomLight;

    [SerializeField]
    private Color normalLightColor = Color.white;

    [SerializeField]
    private Color WarningLightColor = Color.red;

    [SerializeField]
    private Color cursedLightColor = Color.red;

    [SerializeField]
    private float cursedDelay = 1f;

    [SerializeField]
    private float cursedDuration = 5f;

    [SerializeField]
    private float instantDamage = 30f;
    [SerializeField]
    private float instantDamageDelay = 3f;

    private Coroutine instantDamageCoroutine;

    [SerializeField]
    private float OvertimeDamage = 2f;

    [SerializeField]
    private float damageInterval = 0.1f;

    private PlayerController player;
    private Coroutine cursedDamageCoroutine;

    private bool playerInside;

    [SerializeField]
    private float warningTime = 1f;
    private float warningTimer;
    private bool isWarning;

    private void Update()
    {
        if (!isWarning)
            return;

        warningTimer -= Time.deltaTime;

        if (warningTimer <= 0f)
        {
            EndWarning();
        }
    }

    public RoomState GetState()
    {
        return currentState;
    }
    public bool IsNormal()
    {
        return currentState == RoomState.Normal;
    }
    public bool IsWarning()
    {
        return currentState == RoomState.Warning;
    }
    public bool IsCursed()
    {
        return currentState == RoomState.Cursed;
    }
    public bool IsPlayerInside()
    {
        return playerInside;
    }

    public void PlayerEnter()
    {
        playerInside = true;

        player = FindFirstObjectByType<PlayerController>();

        //Debug.Log("Player เข้า " + gameObject.name);

        if (currentState == RoomState.Cursed)
        {
            player.TakeSanityDamage(instantDamage);
            player.BigShake();

            if (instantDamageCoroutine != null)
            {
                StopCoroutine(instantDamageCoroutine);
            }
            instantDamageCoroutine = StartCoroutine(InstantDamageAfterDelay());
        }
    }

    public void PlayerExit()
    {
        playerInside = false;
        player = null;

        if (instantDamageCoroutine != null)
        {
            StopCoroutine(instantDamageCoroutine);
            instantDamageCoroutine = null;
        }
        //Debug.Log("Player ออกจาก " + gameObject.name);
    }

    public void SetWarning()
    {
        currentState = RoomState.Warning;

        warningTimer = warningTime;
        isWarning = true;

        SpawnCursedTrash();
        SetRoomLight(WarningLightColor);
        Debug.Log(gameObject.name + " Warning");
    }
    private void EndWarning()
    {
        isWarning = false;

        SetCursed();
        //Debug.Log(gameObject.name + " Warning จบ");
    }
    public void SetCursed()
    {
        currentState = RoomState.Cursed;
        SetRoomLight(cursedLightColor);
        if (playerInside && player != null)
        {
            if (instantDamageCoroutine != null)
            {
                StopCoroutine(instantDamageCoroutine);
            }

            instantDamageCoroutine = StartCoroutine(InstantDamageAfterDelay());
        }
        //Debug.Log(gameObject.name + " Cursed");
    }
    public void StartCursedImmediately()
    {
        currentState = RoomState.Cursed;

        SetRoomLight(cursedLightColor);

        //Debug.Log(gameObject.name + " Cursed ทันที เพราะ Player อยู่ในห้อง" );
        if (cursedDamageCoroutine != null)
        {
            StopCoroutine(cursedDamageCoroutine);
        }
        cursedDamageCoroutine = StartCoroutine(CursedDamageOvertime());
    }
    public void SetNormal()
    {
        currentState = RoomState.Normal;

        SetRoomLight(normalLightColor);
        //Debug.Log(gameObject.name + " Normal");
    }

    private void SpawnCursedTrash()
    {
        if (cursedTrashPrefab == null)
        {
            return;
        }
        if (trashSpawnPoint == null)
        {
            return;
        }

        GameObject cursedTrashObject = Instantiate(
            cursedTrashPrefab,
            trashSpawnPoint.position,
            trashSpawnPoint.rotation
        );

        CursedTrash cursedTrash = cursedTrashObject.GetComponent<CursedTrash>();

        if (cursedTrash != null)
        {
            cursedTrash.SetRoom(this);
        }
        //Debug.Log("สร้างขยะใน " + gameObject.name);
    }


    private void SetRoomLight(Color color)
    {
        if (roomLight == null)
        {
            return;
        }
        roomLight.color = color;
    }
    public void CleanCursedRoom()
    {
        if (currentState != RoomState.Warning &&
            currentState != RoomState.Cursed)
        {
            return;
        }
        isWarning = false;
        warningTimer = 0f;

        if (instantDamageCoroutine != null)
        {
            StopCoroutine(instantDamageCoroutine);
            instantDamageCoroutine = null;
        }
        SetNormal();
        //Debug.Log(gameObject.name + " ถูกทำความสะอาดแล้ว");
    }
    private IEnumerator CursedDamageOvertime()
    {
        yield return new WaitForSeconds(cursedDelay);

        float damageTimer = 0f;
        float durationTimer = 0f;

        while (durationTimer < cursedDuration)
        {
            durationTimer += Time.deltaTime;

            if (playerInside && player != null)
            {
                bool isMoving = player.GetComponent<Rigidbody>().linearVelocity.sqrMagnitude > 0.01f;

                if (isMoving)
                {
                    if (damageTimer <= 0f)
                    {
                        player.TakeSanityDamage(OvertimeDamage);
                        player.SmallShake();
                        damageTimer = damageInterval;
                    }
                }
            }
            damageTimer -= Time.deltaTime;
            yield return null;
        }
        cursedDamageCoroutine = null;
        //Debug.Log(gameObject.name + " จบช่วง Cursed Movement");
        SetWarning();
    }

    private IEnumerator InstantDamageAfterDelay()
    {
        while (playerInside && player != null &&currentState == RoomState.Cursed)
        {
            yield return new WaitForSeconds(instantDamageDelay);

            if (!playerInside || player == null || currentState != RoomState.Cursed)
            {
                break;
            }
            player.TakeSanityDamage(instantDamage);
            player.BigShake();
            //Debug.Log( gameObject.name + " Player ยังไม่ทำความสะอาดโดนDamage");
        }
        instantDamageCoroutine = null;
    }
}