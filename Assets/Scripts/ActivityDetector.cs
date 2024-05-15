using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class ActivityDetector : MonoBehaviour
{
    // Feel free to add additional class variables here
    OculusSensorReader sensorReader;
    public GameObject cur_act;
    public Dictionary<string, Queue<float>> data;
    public int cnt = 0;
    // Start is called before the first frame update
    void Start()
    {
        sensorReader = new OculusSensorReader();
        cur_act = GameObject.Find("Activity Sign");
        data = new Dictionary<string, Queue<float>>();
        data["controller_right_pos.y"] = new Queue<float>();
        data["controller_right_vel.x"] = new Queue<float>();
        data["controller_right_vel.y"] = new Queue<float>();
        data["controller_right_pos.x"] = new Queue<float>();
        data["headset_pos.y"] = new Queue<float>();
        data["headset_pos.z"] = new Queue<float>();
        data["controller_right_pos.z"] = new Queue<float>();
        data["headset_vel.y"] = new Queue<float>();
        data["controller_left_rot.z"] = new Queue<float>();
    }

    float CalculateStd(IEnumerable<float> values)
    {
        double avg = values.Average();
        return (float) Math.Sqrt(values.Average(v=>Math.Pow(v-avg,2)));
    }

    float CalculateAverage(IEnumerable<float> numbers)
    {
        return numbers.Average();
    }

    void appendData(Dictionary<string, Vector3> attributes) {
        data["controller_right_pos.y"].Enqueue(attributes["controller_right_pos"].y);
        data["controller_right_vel.x"].Enqueue(attributes["controller_right_vel"].x);
        data["controller_right_vel.y"].Enqueue(attributes["controller_right_vel"].y);
        data["controller_right_pos.x"].Enqueue(attributes["controller_right_pos"].x);
        data["headset_pos.y"].Enqueue(attributes["headset_pos"].y);
        data["headset_pos.z"].Enqueue(attributes["headset_pos"].z);
        data["controller_right_pos.z"].Enqueue(attributes["controller_right_pos"].z);
        data["headset_vel.y"].Enqueue(attributes["headset_vel"].y);
        data["controller_left_rot.z"].Enqueue(attributes["controller_left_rot"].z);        
    }

    // Update is called once per frame
    void Update()
    {
        
        sensorReader.RefreshTrackedDevices();
        var attributes = sensorReader.GetSensorReadings();
        
 
        TextMesh t = cur_act.GetComponent<TextMesh> ();
        bool aButtonFirstPressed = OVRInput.GetDown(OVRInput.Button.One);
        bool aButtonPressed = OVRInput.Get(OVRInput.Button.One);
        if (aButtonFirstPressed){
            foreach(var item in data.Values)
            {
                item.Clear();
            }
        }
        if (aButtonPressed){
            appendData(attributes);
            t.text = CalculateAverage(data["controller_right_pos.y"]).ToString("0.0000");
        }else{
            t.text = "Waiting";
        }
    }
}