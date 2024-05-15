using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class ActivityDetector : MonoBehaviour
{
    // Feel free to add additional class variables here
    OculusSensorReader sensorReader;
    public GameObject cur_act;
    public GameObject court;
    public Dictionary<string, List<float>> data;
    public int cnt = 0;
    float racketlen = 0.68f;
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

        data["racket.x"] = new List<float>();
        data["racket.y"] = new List<float>();
        data["racket.z"] = new List<float>();
    }

    //function to get up vector so that simulated position of racket can be found.
    public static System.Numerics.Vector3 GetUpVector(float yawDegrees, float pitchDegrees, float rollDegrees)
    {
        float yaw = MathF.PI / 180 * yawDegrees;
        float pitch = MathF.PI / 180 * pitchDegrees;
        float roll = MathF.PI / 180 * rollDegrees;

        System.Numerics.Matrix4x4 yawMatrix = System.Numerics.Matrix4x4.CreateRotationY(yaw);
        System.Numerics.Matrix4x4 pitchMatrix = System.Numerics.Matrix4x4.CreateRotationX(pitch);
        System.Numerics.Matrix4x4 rollMatrix = System.Numerics.Matrix4x4.CreateRotationZ(roll);

        System.Numerics.Matrix4x4 matrix = System.Numerics.Matrix4x4.Multiply(System.Numerics.Matrix4x4.Multiply(yawMatrix, pitchMatrix), rollMatrix);

        System.Numerics.Vector3 initialUp = new System.Numerics.Vector3(0, 1, 0);

        System.Numerics.Vector3 transformedUp = System.Numerics.Vector3.Transform(initialUp, matrix);

        return transformedUp;
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

    void appendData(Dictionary<string, UnityEngine.Vector3> attributes) {
        data["controller_right_pos.y"].Add(attributes["controller_right_pos"].y);
        data["controller_right_vel.x"].Add(attributes["controller_right_vel"].x);
        data["controller_right_vel.y"].Add(attributes["controller_right_vel"].y);
        data["controller_right_pos.x"].Add(attributes["controller_right_pos"].x);
        data["controller_right_pos.z"].Add(attributes["controller_right_pos"].z);
        data["controller_right_vel.z"].Add(attributes["controller_right_vel"].z);

        data["headset_pos.y"].Add(attributes["headset_pos"].y);
        data["headset_pos.z"].Add(attributes["headset_pos"].z);
        
        data["headset_vel.y"].Add(attributes["headset_vel"].y);

        System.Numerics.Vector3 UpVector = GetUpVector(attributes["control_right_rot"].y, attributes["control_right_rot"].x, attributes["control_right_rot"].z);
        data["racket.x"].Add(attributes["controller_right_pos"].x + UpVector.X * racketlen);
        data["racket.y"].Add(attributes["controller_right_pos"].y + UpVector.Y * racketlen);
        data["racket.z"].Add(attributes["controller_right_pos"].z + UpVector.Z * racketlen);
    }
    //outputs maximimum velocity of simulated racket head in dictionary as a stirng
    string MaxVelocity()
    {
        float lastx = data["racket.x"][0];
        float lasty = data["racket.y"][0];
        float lastz = data["racket.z"][0];
        float dx, dy, dz, vel;
        float maxvel = 0;
        for(int i=1; i < data["racket.x"].Count; i++)
        {
            dx = data["racket.x"][i]-lastx;
            dy = data["racket.y"][i]-lasty;
            dz = data["racket.z"][i]-lastz;
            vel = (float) Math.Sqrt(
                Math.Pow(dx, 2) +
                Math.Pow(dy, 2) +
                Math.Pow(dz, 2)
            );
            maxvel = Math.Max(maxvel, vel);
            lastx = data["racket.x"][i];
            lasty = data["racket.y"][i];
            lastz = data["racket.z"][i];
        }
        return string.Format("{0:N2}", maxvel);
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
            //t.text = res + " innit?";
            PlayAudio(res);
            //t.text = AnalyzeSwing()+" innit?" + "\n Vel:" + MaxVelocity();
        }
    }
}