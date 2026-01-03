using UnityEngine;

namespace SuperGear
{
    public class ReceivingBinControl : MonoBehaviour
    {
        
        [Header("接收柱配置")]
        [Tooltip("接收柱的世界坐标（自动同步Transform，可手动微调）")]
        [SerializeField] private Vector3 ReceivingBinworldPosition; 
        [SerializeField] public bool isOccupied; // 是否被使用

        private void Awake()
        {
            InitializeReceivingBin();
        }
        private void InitializeReceivingBin()
        {
            ReceivingBinworldPosition = transform.position;
            isOccupied = false;
        }


    }
}