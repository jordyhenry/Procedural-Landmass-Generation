using UnityEngine;

public class HideOnPlay : MonoBehaviour
{
    public void Start()
    {
        gameObject.SetActive(false);
    }
}
