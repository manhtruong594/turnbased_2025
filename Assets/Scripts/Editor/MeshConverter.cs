using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class SkinnedMeshConverter : MonoBehaviour
{
    [Tooltip("Tích vào đây nếu bạn muốn lưu Mesh thành một file .asset trong Project")]
    public bool saveMeshAsAsset = true; 

    [ContextMenu("Bake to Static Mesh")]
    public void ConvertToMeshRenderer()
    {
        SkinnedMeshRenderer skinnedMesh = GetComponent<SkinnedMeshRenderer>();
        
        if (skinnedMesh == null)
        {
            Debug.LogWarning("Không tìm thấy SkinnedMeshRenderer trên GameObject này.");
            return;
        }

        // 1. Tạo Mesh và nướng hình dáng hiện tại
        Mesh bakedMesh = new Mesh();
        skinnedMesh.BakeMesh(bakedMesh);
        bakedMesh.name = skinnedMesh.sharedMesh.name + "_Baked";

        // Lưu lại material đang dùng
        Material[] materials = skinnedMesh.sharedMaterials;

#if UNITY_EDITOR
        // 2. Xử lý lưu thành file Asset cứng
        if (saveMeshAsAsset)
        {
            // Mở cửa sổ yêu cầu người dùng chọn thư mục lưu file
            string path = EditorUtility.SaveFilePanelInProject(
                "Lưu Baked Mesh",
                bakedMesh.name + ".asset",
                "asset",
                "Chọn thư mục trong Project để lưu file Mesh này"
            );

            // Nếu người dùng bấm Cancel trong cửa sổ lưu file
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Đã hủy quá trình lưu Mesh. Chuyển đổi bị hủy.");
                return; 
            }

            // Tạo file .asset vật lý trong project
            AssetDatabase.CreateAsset(bakedMesh, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Rất quan trọng: Phải load lại cái mesh vừa lưu ra để gán vào MeshFilter
            // Nếu không, MeshFilter vẫn sẽ tham chiếu đến cái mesh trên RAM
            bakedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }
#endif

        // 3. Xóa SkinnedMeshRenderer cũ
        DestroyImmediate(skinnedMesh);

        // 4. Gắn MeshFilter và gán Baked Mesh
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = bakedMesh;

        // 5. Gắn MeshRenderer và gán Materials
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = materials;

        Debug.Log("Đã chuyển đổi thành công! " + (saveMeshAsAsset ? "Đã lưu thành file Asset." : "Mesh chỉ lưu tạm trên Scene."));
    }
}