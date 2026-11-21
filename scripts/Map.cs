using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    [Header("Tham chiếu Player")]
    public Transform player;

    [Header("Danh sách các Map (không cần có sảnh)")]
    public GameObject[] maps;

    [Tooltip("Kéo 'sanhgame' vào đây để sảnh luôn bị tắt khi qua map")]
    public GameObject lobbyMap;

    public float spawnHeightOffset = 0.1f;

    [Header("Thông tin hiện tại")]
    public int currentMapIndex = -1;

    public Transform CurrentMapRoot
    {
        get
        {
            if (maps != null && currentMapIndex >= 0 && currentMapIndex < maps.Length && maps[currentMapIndex] != null)
                return maps[currentMapIndex].transform;
            return null;
        }
    }

    List<Transform> GetSpawnPoints(GameObject map)
    {
        List<Transform> list = new List<Transform>();
        foreach (Transform child in map.GetComponentsInChildren<Transform>(true))
        {
            if (child == map.transform) continue;
            if (!child.name.StartsWith("Spawn_")) continue;

            // Bỏ qua object "chứa" các điểm spawn khác
            bool isContainer = false;
            foreach (Transform c in child)
                if (c.name.StartsWith("Spawn_")) { isContainer = true; break; }

            if (!isContainer) list.Add(child);
        }
        return list;
    }

    bool IsLobby(GameObject map)
    {
        if (map == lobbyMap) return true;
        return lobbyMap == null && map.name.ToLower().Contains("sanh");
    }

    // Trả về true nếu dịch chuyển thành công
    public bool ChangeRandomMap()
    {
        if (player == null || maps == null || maps.Length == 0)
        {
            Debug.LogError("Chua gan Player hoac danh sach Map!");
            return false;
        }

        // Chỉ chọn trong các map CÓ SpawnPoint và KHÔNG phải sảnh
        List<int> candidates = new List<int>();
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i] == null || IsLobby(maps[i])) continue;
            if (GetSpawnPoints(maps[i]).Count > 0) candidates.Add(i);
            else Debug.LogWarning("Map " + maps[i].name + " khong co object ten bat dau bang 'Spawn' -> bi bo qua");
        }

        if (candidates.Count == 0)
        {
            Debug.LogError("Khong co map nao hop le (can co object con ten Spawn_...)!");
            return false;
        }

        // Tránh trùng map vừa ở (nếu còn lựa chọn khác)
        if (candidates.Count > 1) candidates.Remove(currentMapIndex);

        int mapIndex = candidates[Random.Range(0, candidates.Count)];
        GameObject selectedMap = maps[mapIndex];
        List<Transform> spawnPoints = GetSpawnPoints(selectedMap);

        // Tắt tất cả map + sảnh, bật map được chọn
        foreach (GameObject m in maps) if (m != null) m.SetActive(false);
        if (lobbyMap != null) lobbyMap.SetActive(false);
        selectedMap.SetActive(true);
        Physics.SyncTransforms();

        // Thử các điểm spawn theo thứ tự ngẫu nhiên, chỉ nhận điểm CÓ MẶT ĐẤT bên dưới
        Transform destination = null;
        Vector3 spawnPos = Vector3.zero;
        List<Transform> pool = new List<Transform>(spawnPoints);

        while (pool.Count > 0)
        {
            int i = Random.Range(0, pool.Count);
            Transform sp = pool[i];
            pool.RemoveAt(i);

            if (Physics.Raycast(sp.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 30f, ~0, QueryTriggerInteraction.Ignore))
            {
                destination = sp;
                spawnPos = hit.point + Vector3.up * spawnHeightOffset;
                break;
            }
            Debug.LogWarning("Spawn '" + sp.name + "' khong co mat dat o duoi, vi tri " + sp.position);
        }

        if (destination == null)
        {
            Debug.LogError("Map " + selectedMap.name + ": khong co Spawn nao co mat dat!");
            selectedMap.SetActive(false);
            if (lobbyMap != null) lobbyMap.SetActive(true); // quay lại sảnh, không để rơi
            return false;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        player.SetPositionAndRotation(spawnPos, Quaternion.Euler(0f, destination.eulerAngles.y, 0f));
        Physics.SyncTransforms();

        if (controller != null) controller.enabled = true;

        global::player p = player.GetComponent<global::player>();
        if (p != null) { p.ResetVelocity(); p.SetSpawnGrace(0.5f); }

        currentMapIndex = mapIndex;
        Debug.Log("Map: " + selectedMap.name + " | Spawn: " + destination.name + " | Pos: " + spawnPos);
        return true;
    }
}