using DB;
using UnityEngine;

namespace Items
{
    public class MaskItem : ItemBase
    {
        [Header("Mask Result")]
        [SerializeField] private string orderId;

        [SerializeField] private DBMask.MaskData targetMaskData;
        [SerializeField] private DBMask.MaskData actualMaskData;

        public string OrderId => orderId;
        public DBMask.MaskData TargetMaskData => targetMaskData;
        public DBMask.MaskData ActualMaskData => actualMaskData;

        public void Init(DBMask.MaskData targetMask, DBMask.MaskData actualMask)
        {
            targetMaskData = targetMask;
            actualMaskData = actualMask;
            orderId = targetMask.OR_Id;
        }
    }
}