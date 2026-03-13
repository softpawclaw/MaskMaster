using DB;
using UnityEngine;

namespace Player
{
    public class PlayerHandsController : MonoBehaviour
    {
        //plug
        public void GiveRecipe(DBMask.MaskData targetMask)
        {
            Debug.Log($"Got mask recipe {targetMask.Id}");
        }
    }
}