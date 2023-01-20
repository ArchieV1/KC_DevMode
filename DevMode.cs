using UnityEngine;

namespace KC_DevMode
{
    public class DevMode : MonoBehaviour
    {
        public void Start()
        {
            // Delete update news
            Destroy(GameObject.Find("UpdateContainer"));
            Destroy(GameObject.Find("UpdateDescription"));
        }
    }
}