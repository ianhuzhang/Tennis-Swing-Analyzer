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
    public Dictionary<string, List<float>> data;
    public int cnt = 0;
    float racketlen = .68;
    // Start is called before the first frame update
    void Start()
    {
        sensorReader = new OculusSensorReader();
        cur_act = GameObject.Find("Activity Sign");
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
    public static Vector3 GetUpVector(float yawDegrees, float pitchDegrees, float rollDegrees)
    {
        float yaw = MathF.PI / 180 * yawDegrees;
        float pitch = MathF.PI / 180 * pitchDegrees;
        float roll = MathF.PI / 180 * rollDegrees;

        Matrix4x4 yawMatrix = Matrix4x4.CreateRotationY(yaw);
        Matrix4x4 pitchMatrix = Matrix4x4.CreateRotationX(pitch);
        Matrix4x4 rollMatrix = Matrix4x4.CreateRotationZ(roll);

        Matrix4x4 matrix = Matrix4x4.Multiply(Matrix4x4.Multiply(yawMatrix, pitchMatrix), rollMatrix);

        Vector3 initialUp = new Vector3(0, 1, 0);

        Vector3 transformedUp = Vector3.Transform(initialUp, matrix);

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

        UpVector = GetUpVector(attributes["control_right_rot"].y, attributes["control_right_rot"].x, attributes["control_right_rot"].z);
        data["racket.x"] = attributes["controller_right_pos"].x + UpVector.x * racketlen;
        data["racket.y"] = attributes["controller_right_pos"].y + UpVector.y * racketlen;
        data["racket.z"] = attributes["controller_right_pos"].z + UpVector.z * racketlen;
    }
    //outputs maximimum velocity of simulated racket head in dictionary as a stirng
    string MaxVelocity()
    {
        float lastx = data["racket.x"][0];
        float lasty = data["racket.y"][0];
        float lastz = data["racket.z"][0];
        float dx, dy, dz, vel;
        maxvel = 0
        for(int i=1; i < data["racket.x"].Count; i++)
        {
            dx = data["racket.x"][i]-lastx;
            dy = data["racket.y"][i]-lasty;
            dz = data["racket.z"][i]-lastz;
            vel = Math.Sqrt(
                Math.Pow(dx, 2) +
                Math.Pow(dy, 2) +
                Math.Pow(dz, 2)
            );
            maxvel = max(maxvel, vel);
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
                return "it was a forehand";
            else
                return "it was a backhand";
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
                return "it was a serve";
            else
                return "it was a volley";
        }
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
            t.text = AnalyzeSwing()+" innit?" + "\n Vel:" + MaxVelocity();
        }
    }
}