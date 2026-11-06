using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MeshSortingOrder : MonoBehaviour
{
    [Header("2D Sorting Order")]
    public string sortingLayerName = "Default"; 
    public int sortingOrder = -100;              

    void Awake()
    {
        ApplySortingOrder();
    }

    void OnValidate()
    {
        ApplySortingOrder();
    }

    private void ApplySortingOrder()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = sortingOrder;
        }
    }
}
