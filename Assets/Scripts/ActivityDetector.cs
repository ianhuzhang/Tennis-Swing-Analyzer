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
    public float racketlen = 0.68f;
    public string cur_action = "";
    public string sound = "Erm";
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

        data["headset_pos.x"] = new List<float>();
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
        data["controller_right_vel.x"].Add(attributes["controller_right_vel"].x);
        data["controller_right_vel.y"].Add(attributes["controller_right_vel"].y);
        data["controller_right_vel.z"].Add(attributes["controller_right_vel"].z);

        data["controller_right_pos.x"].Add(attributes["controller_right_pos"].x);
        data["controller_right_pos.y"].Add(attributes["controller_right_pos"].y);
        data["controller_right_pos.z"].Add(attributes["controller_right_pos"].z);

        // Calculate the magnitude of the right controller's velocity vector
        data["controller_right_vel"].Add( (float) Math.Sqrt(
                Math.Pow(attributes["controller_right_vel"].x, 2) +
                Math.Pow(attributes["controller_right_vel"].y, 2) +
                Math.Pow(attributes["controller_right_vel"].z, 2)
        ));

        data["headset_pos.x"].Add(attributes["headset_pos"].x);
        data["headset_pos.y"].Add(attributes["headset_pos"].y);
        data["headset_pos.z"].Add(attributes["headset_pos"].z);
        
        data["headset_vel.y"].Add(attributes["headset_vel"].y);

        System.Numerics.Vector3 UpVector = GetUpVector(attributes["controller_right_rot"].y, attributes["controller_right_rot"].x, attributes["controller_right_rot"].z);
        data["racket.x"].Add(attributes["controller_right_pos"].x + UpVector.X * racketlen);
        data["racket.y"].Add(attributes["controller_right_pos"].y + UpVector.Y * racketlen);
        data["racket.z"].Add(attributes["controller_right_pos"].z + UpVector.Z * racketlen);

    }
    //outputs maximimum velocity of simulated racket head in dictionary as a stirng
    string GetMaxVelocity()
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
        return string.Format("{0:N2} m/s", maxvel*72);
    }

    (int start, int end) GetStartEndAfterClassification()
    {
        int start = 0;
        int end = data["controller_right_vel.z"].Count -1;

        int pos = -1;
        int neg = -1;
        float min = float.MaxValue;
        float max = float.MinValue;
        int apex = -1;
        float maxApex = float.MinValue;

        for (int i = 0; i < data["controller_right_vel"].Count; i++)
        {
            if (data["controller_right_vel"][i] > maxApex)
            {
                apex = i;
                maxApex = data["controller_right_vel"][i];
            }
        }

        for (int i = 0; i < apex; i++)
        {
            if (data["controller_right_vel.z"][i] > max)
            {
                max = data["controller_right_vel.z"][i];
                pos = i;
            }
        }

        for (int i = apex; i < data["controller_right_vel.z"].Count; i++)
        {
            if (data["controller_right_vel.z"][i] < min)
            {
                min = data["controller_right_vel.z"][i];
                neg = i;
            }
        }

        for (int i = pos; i > 0; i--)
        {
            if (data["controller_right_vel.z"][i] >= 0.2f && data["controller_right_vel.z"][i - 1] < 0.2f)
            {
                start = i;
                break;
            }
        }

        if (cur_action != "volley")
        {
            for (int i = neg; i < data["controller_right_vel.z"].Count - 1; i++)
            {
                if (data["controller_right_vel.z"][i] <= -0.2f && data["controller_right_vel.z"][i + 1] > -0.2f)
                {
                    end = i + 1;
                    break;
                }
            }
        }
        else
        {
            for (int i = pos; i < data["controller_right_vel.z"].Count - 1; i++)
            {
                if (data["controller_right_vel.z"][i] >= 0.2f && data["controller_right_vel.z"][i + 1] < 0.2f)
                {
                    end = i + 1;
                    break;
                }
            }
        }

        return (start, end);
    }
    float GetRotation(){
        var cur = GetStartEndAfterClassification();
        int start = cur.Item1;
        int end = cur.Item2;
        float res = 0;
        
        for (int i = start; i < end-2;i++){
            var vec1 = new float[] {data["controller_right_pos.x"][i] - data["headset_pos.x"][i], data["controller_right_pos.z"][i] - data["headset_pos.z"][i], 0};
            var vec2 = new float[] {data["controller_right_pos.x"][i+1] - data["headset_pos.x"][i+1], data["controller_right_pos.z"][i+1] - data["headset_pos.z"][i+1], 0};
            float dot = vec1[0] * vec2[0] + vec1[1] * vec2[1];
            float mag1 = (float)Math.Sqrt(vec1[0] * vec1[0] + vec1[1] * vec1[1]);
            float mag2 = (float)Math.Sqrt(vec2[0] * vec2[0] + vec2[1] * vec2[1]);

            float crossZ = vec1[0] * vec2[1] - vec1[1] * vec2[0]; // 2D cross product in the Z direction
            
            if (cur_action == "forehand")
            {
                if (crossZ > 0)
                {
                    res += (float) Math.Acos(dot / mag1 / mag2);
                }
            }
            else if (cur_action == "backhand")
            {
                if (crossZ < 0)
                {
                    res += (float) Math.Acos(dot / mag1 / mag2);
                }
            }
            else
            {
                res += (float) Math.Acos(dot / mag1 / mag2);
            }
            
        }

        return res * 180 / (float) Math.PI;
    }

    //debug
    string tup(){
        var cur = GetStartEndAfterClassification();
        int start = cur.Item1;
        int end = cur.Item2;
        var tup = new float[]
        {
            data["controller_right_pos.x"][end] - data["headset_pos.x"][end],
            data["controller_right_pos.z"][end] - data["headset_pos.z"][end],
            data["controller_right_pos.y"][end] - data["headset_pos.y"][end]
        };
        return string.Join(", ", tup);
    }
    string GetFollowThrough()
    {
        var cur = GetStartEndAfterClassification();
        int start = cur.Item1;
        int end = cur.Item2;
        if (end - start < 10) {
            sound = "Erm";
            return "Are you even trying?";
        }
        var tup = new float[]
        {
            data["controller_right_pos.x"][end] - data["headset_pos.x"][end],
            data["controller_right_pos.z"][end] - data["headset_pos.z"][end],
            data["controller_right_pos.y"][end] - data["headset_pos.y"][end]
        };

        if (cur_action == "forehand")
        {
            if (tup[2] < -0.2f)
            {
                sound = "shoulder";
                return "Try to follow-through a bit higher, over your shoulder!";
            }
            if (tup[1] > 0)
            {
                sound = "furtherback";
                return "Try to end your swing further back!";
            }
            if (tup[0] > 0)
            {
                sound = "left";
                return "Make sure to complete your follow-through on the left side of your body!";
            }
            sound = "forehand";
            return "Nice follow through!";
        }

        if (cur_action == "backhand")
        {
            if (tup[2] < -0.2f)
            {
                sound = "shoulder";
                return "Try to follow-through a bit higher, over your shoulder!";
            }
            if (tup[1] > 0)
            {
                sound = "furtherback";
                return "Try to end your swing further back!";
            }
            if (tup[0] < 0)
            {
                sound = "right";
                return "Make sure to complete your follow-through on the right side of your body!";
            }
            sound = "backhand";
            return "Nice follow through!";
        }

        if (cur_action == "serve")
        {
            if (-1 * tup[2] < data["controller_right_pos.y"].Max() - data["headset_pos.y"].Max())
            {
                sound = "waist";
                return "Try to finish a bit lower, near your waist!";
            }
            if (tup[1] > 0)
            {
                sound = "furtherback";
                return "Try to end your swing further back!";
            }
            if (tup[0] > 0)
            {
                sound = "badserve";
                return "Make sure to complete your follow-through on the left side of your body!";
            }
            sound = "serve";
            return "Nice follow through!";

        }

        if (cur_action == "volley")
        {
            sound = "volley";
            return "Nice volley!";
        }
        return "error";
    }

    string AnalyzeSwing()
    {
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
            t.text = "Recording";
            sound = "Erm";
        }
        if (aButtonPressed){
            appendData(attributes);
        }
        if (aButtonReleased){
            cur_action = AnalyzeSwing();
            t.text = cur_action+" innit?";
            t.text +=  "\n Vel:" + GetMaxVelocity();
            //t.text += "\n S/E:" + GetStartEndAfterClassification();
            t.text += "\n Rot:" + GetRotation();
            //t.text += "\n Tup:" + tup();
            t.text += "\n Feedback:" + GetFollowThrough();
            PlayAudio(sound);
        }
    }
}