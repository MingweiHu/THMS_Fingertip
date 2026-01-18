using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class text : MonoBehaviour
{

    public Text m_Text;
    // Start is called before the first frame update
    void Start()
    {
        m_Text = GetComponent<Text>();
        m_Text.text = "increase volume";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
