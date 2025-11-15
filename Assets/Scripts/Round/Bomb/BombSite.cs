using UnityEngine;
using Photon.Pun;

public class BombSite : MonoBehaviour
{
    [Header("Bomb Site Settings")]
    public string siteName = "A"; // A або B
    public float radius = 5f;
    public Color siteColor = Color.yellow;
    
    [Header("Visual")]
    public bool showGizmos = true;
    public GameObject siteMarker; // Опціональний 3D маркер на землі
    
    private bool playerInSite = false;
    private BombUI bombUI;

    void Start()
    {
        bombUI = FindObjectOfType<BombUI>();
        
        // Створюємо візуальний маркер якщо його немає
        if (siteMarker == null)
        {
            CreateVisualMarker();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            playerInSite = true;
            
            // Перевіряємо чи це терорист з бомбою
            if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("team"))
            {
                int team = (int)PhotonNetwork.LocalPlayer.CustomProperties["team"];
                if (team == 0) // T команда
                {
                    bool hasBomb = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("hasBomb") 
                        && (bool)PhotonNetwork.LocalPlayer.CustomProperties["hasBomb"];
                    
                    if (hasBomb && bombUI != null)
                    {
                        bombUI.OnPlayerEnteredBombSite(siteName);
                    }
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            playerInSite = false;
            
            if (bombUI != null)
            {
                bombUI.OnPlayerLeftBombSite(siteName);
            }
        }
    }

    private void CreateVisualMarker()
    {
        // Створюємо простий циліндр як візуальний маркер зони
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.transform.SetParent(transform);
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = new Vector3(radius * 2, 0.1f, radius * 2);
        
        // Налаштовуємо матеріал
        Renderer renderer = marker.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(siteColor.r, siteColor.g, siteColor.b, 0.3f);
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        renderer.material = mat;
        
        // Видаляємо колайдер з маркера (він на батьківському об'єкті)
        Destroy(marker.GetComponent<Collider>());
        
        siteMarker = marker;
        
        // Додаємо текст над зоною
        CreateSiteLabel();
    }

    private void CreateSiteLabel()
    {
        GameObject labelObj = new GameObject($"Label_{siteName}");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = Vector3.up * 2f;
        
        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = $"BOMB SITE {siteName}";
        textMesh.fontSize = 50;
        textMesh.color = siteColor;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        // Щоб текст був завжди видимий
        labelObj.transform.localScale = Vector3.one * 0.1f;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Малюємо сферу радіусу
        Gizmos.color = siteColor;
        Gizmos.DrawWireSphere(transform.position, radius);
        
        // Малюємо назву точки
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"Bomb Site {siteName}");
        #endif
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // Малюємо заповнену сферу коли вибрано
        Gizmos.color = new Color(siteColor.r, siteColor.g, siteColor.b, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }

    public bool IsPlayerInSite()
    {
        return playerInSite;
    }
}