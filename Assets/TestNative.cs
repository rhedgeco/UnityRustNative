using UnityEngine;

public class TestNative : MonoBehaviour
{
    void Start()
    {
        Debug.Log(RustNative.NewRustNativeProject.add_one(1));
    }
}
