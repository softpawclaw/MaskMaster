using System;
using UnityEngine;

namespace Systems
{
    public abstract class ExecuteSystemBase : MonoBehaviour
    {
        public abstract void Execute(string id, Action completeAction);
    }
}