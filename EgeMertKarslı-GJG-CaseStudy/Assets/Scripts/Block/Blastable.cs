using UnityEngine;
using UnityEngine.EventSystems;

public class Blastable : MonoBehaviour, IPointerClickHandler
{
    private BlockMetadata blockData;

    private void Awake()
    {
        blockData = GetComponent<BlockMetadata>();
    }

    private void OnEnable()
    {
        blockData = GetComponent<BlockMetadata>();       
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        BlastManager.Instance.OnBlockClicked(blockData.GridX, blockData.GridY);
    }
}