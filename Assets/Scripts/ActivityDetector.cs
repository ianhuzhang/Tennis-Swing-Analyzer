using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class ActivityDetector : MonoBehaviour
{
    // Feel free to add additional class variables here
    OculusSensorReader sensorReader;
    public GameObject cur_act;
    public GameObject court;
    public Dictionary<string, List<float>> data;
    AudioSource myAudioSource = new AudioSource();

    // Start is called before the first frame update
    void Start()
    {
        sensorReader = new OculusSensorReader();
        cur_act = GameObject.Find("Activity Sign");
        court = GameObject.Find("court");
        data = new Dictionary<string, List<float>>();
        
        data["controller_right_pos.y"] = new List<float>();
        data["controller_right_pos.x"] = new List<float>();
        data["controller_right_pos.z"] = new List<float>();

        data["controller_right_vel.x"] = new List<float>();
        data["controller_right_vel.y"] = new List<float>();
        data["controller_right_vel.z"] = new List<float>();

        data["controller_right_vel"] = new List<float>();

        data["headset_pos.y"] = new List<float>();
        data["headset_pos.z"] = new List<float>();
        
        data["headset_vel.y"] = new List<float>();
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
        data["controller_right_pos.y"].Add(attributes["controller_right_pos"].y);
        data["controller_right_vel.x"].Add(attributes["controller_right_vel"].x);
        data["controller_right_vel.y"].Add(attributes["controller_right_vel"].y);
        data["controller_right_pos.x"].Add(attributes["controller_right_pos"].x);
        data["controller_right_pos.z"].Add(attributes["controller_right_pos"].z);
        data["controller_right_vel.z"].Add(attributes["controller_right_vel"].z);

        data["headset_pos.y"].Add(attributes["headset_pos"].y);
        data["headset_pos.z"].Add(attributes["headset_pos"].z);
        
        data["headset_vel.y"].Add(attributes["headset_vel"].y);
    }

    string AnalyzeSwing()
    {
        // Calculate the magnitude of the right controller's velocity vector
        for (int i = 0; i < data["controller_right_vel.x"].Count; i++)
        {
            data["controller_right_vel"].Add( (float) Math.Sqrt(
                Math.Pow(data["controller_right_vel.x"][i], 2) +
                Math.Pow(data["controller_right_vel.y"][i], 2) +
                Math.Pow(data["controller_right_vel.z"][i], 2)
            ));
        }

        // Find the index of the maximum velocity
        int idxMax = -1;
        double maxVelocity = Double.NegativeInfinity;
        for (int i = 0; i < data["controller_right_vel"].Count; i++)
        {
            int index = i;
            if (data["controller_right_vel"][index] > maxVelocity)
            {
                maxVelocity = data["controller_right_vel"][index];
                idxMax = index;
            }
        }

        // Analyze the direction of the swing at the point of maximum velocity
        if (data["controller_right_vel.y"][idxMax] > 0) // If swing moves upwards
        {
            // Determine if swing is FHD or BHD based on left/right movement
            if (data["controller_right_vel.x"][idxMax] < 0) // If swing moves left
                return "forehand";
            else
                return "backhand";
        }
        else // If swing does not move upwards
        {
            // Check relative position of the controller to the headset
            double maxHeightDifference = 0;
            for (int i = 0; i < data["controller_right_pos.y"].Count; i++)
            {
                double heightDifference = data["controller_right_pos.y"][i] - data["headset_pos.y"][i];
                if (heightDifference > maxHeightDifference)
                    maxHeightDifference = heightDifference;
            }

            if (maxHeightDifference > 0.2) // Threshold to determine SRV or VOL
                return "serve";
            else
                return "volley";
        }
    }

    void PlayAudio(string filename)
    {
        AudioSource myAudioSource = cur_act.GetComponent<AudioSource> ();
        myAudioSource.clip = Resources.Load<AudioClip>("Audio/" + filename);
        myAudioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        sensorReader.RefreshTrackedDevices();
        var attributes = sensorReader.GetSensorReadings();
        
        TextMesh t = cur_act.GetComponent<TextMesh> ();
        
        bool aButtonFirstPressed = OVRInput.GetDown(OVRInput.Button.One);
        bool aButtonReleased = OVRInput.GetUp(OVRInput.Button.One);
        bool aButtonPressed = OVRInput.Get(OVRInput.Button.One);
        if (aButtonFirstPressed){
            foreach(var item in data.Values)
            {
                item.Clear();
            }
        }
        if (aButtonPressed){
            appendData(attributes);
            //t.text = CalculateAverage(data["controller_right_pos.y"]).ToString("0.0000");
            t.text = "Recording";
        }
        if (aButtonReleased){
            string res = AnalyzeSwing();
            t.text = res + " innit?";
            PlayAudio(res);
        }
    }
}