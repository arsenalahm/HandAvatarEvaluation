using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;

public class SeahorseBodyMapping : MonoBehaviour
{
    public GameObject avatar;
    public GameObject user;
    public GameObject anotherUser;

    public string mapping_file = "./Assets/spider_mapping_baseline.txt";
    private Dictionary<GameObject, GameObject> mapping = new Dictionary<GameObject, GameObject>();
    private Dictionary<string, Quaternion> initialRotations = new Dictionary<string, Quaternion>();
    private Dictionary<string, Quaternion> initialUserRotations = new Dictionary<string, Quaternion>();
    private List<string> controlledJoints = new List<string>();
    private List<string> controlledUserJoints = new List<string>();
    private Dictionary<string, Quaternion> clusterPoseRotations = new Dictionary<string, Quaternion>();
    private List<float> poseDeviations = new List<float>();

    public PlayAnimation player;
    public RecordAvatar recorder;

    public TextMeshProUGUI text;

    public string userName = "";
    private StreamWriter writer;

    private string poseFile = "seahorse_user_pose.txt";
    
    private bool flag = false;
    private bool poseFlag = false;

    private float timer = 0f;
    private float recordTimer = 0f;

    private string clusterFile = "./cluster_poses/seahorse_poses.txt";
    private List<Dictionary<string, List<float>>> clusterPoses = new List<Dictionary<string, List<float>>>();
    private int clusterPoseCnt = 0;

    public GameObject anotherAvatar;
    
    private float bestDeviation = 1e4f;

    string ConvertTransformToString(Transform trans)
    {
        string temp = trans.name;
        for (int i = 0; i < 3; i++)
        {
            temp += " " + trans.position[i];
        }
        for (int i = 0; i < 4; i++)
        {
            temp += " " + trans.localRotation[i];
        }
        return temp;
    }

    string ConvertQuaternionToString(Quaternion trans)
    {
        string temp = "";
        for (int i = 0; i < 4; i++)
        {
            temp += trans[i];
            if (i < 3)
            {
                temp += " ";
            }
        }
        return temp;
    }

    void readMapping()
    {
        StreamReader reader = new StreamReader(mapping_file);
        string content = reader.ReadToEnd();
        reader.Close();
        string[] pairs = content.Split("\n");
        foreach (string pair in pairs)
        {
            string[] joints = pair.Split(": ");
            if (joints.Length != 2)
            {
                continue;
            }
            GameObject ajoint = null;
            GameObject hjoint = null;
            foreach (Transform g in avatar.transform.GetComponentsInChildren<Transform>())
            {
                if (g.name == joints[0])
                {
                    ajoint = g.gameObject;
                    break;
                }
            }
            foreach (Transform g in user.transform.GetComponentsInChildren<Transform>())
            {
                if (g.name == joints[1])
                {
                    hjoint = g.gameObject;
                    break;
                }
            }
            if (ajoint != null && hjoint != null)
            {
                mapping.Add(ajoint, hjoint);
                controlledJoints.Add(ajoint.transform.name);
                initialRotations.Add(joints[0], ajoint.transform.localRotation);
            }
        }
    }
    
    void ReadUserJoints() 
    {        
        StreamReader reader = new StreamReader(mapping_file);
        string content = reader.ReadToEnd();
        reader.Close();
        string[] pairs = content.Split("\n");
        foreach (string pair in pairs)
        {
            string[] joints = pair.Split(": ");
            if (joints.Length != 2)
            {
                continue;
            }
            controlledUserJoints.Add(joints[1]);
        }
    }

    void ReadClusterPoses()
    {
        StreamReader reader = new StreamReader(clusterFile);
        string content = reader.ReadToEnd();
        reader.Close();
        string[] poses = content.Split("#");
        foreach (string pose in poses)
        {
            string[] pairs = pose.Split("\n");
            Dictionary<string, List<float>> tt = new Dictionary<string, List<float>>();
            foreach (string pair in pairs)
            {
                string[] data = pair.Split(" ");
                if (data.Length == 8)
                {
                    List<float> t = new List<float>();
                    for (int i = 1; i < 8; i++)
                    {
                        t.Add(float.Parse(data[i]));
                    }
                    tt.Add(data[0], t);
                }
            }
            clusterPoses.Add(tt);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        text.text = "Perform the human gesture";
        userName += "_seahorse_body.txt";
        writer = new StreamWriter(userName);
        StreamReader reader = new StreamReader(poseFile);
        string[] content = reader.ReadToEnd().Split("\n");
        foreach (string s in content)
        {
            string[] information = s.Split(" ");
            foreach (Transform g in anotherUser.GetComponentsInChildren<Transform>())
            {
                if (g.name == information[0])
                {
                    g.position = new Vector3(float.Parse(information[1]), float.Parse(information[2]), float.Parse(information[3]));
                    g.rotation = new Quaternion(float.Parse(information[4]), float.Parse(information[5]), float.Parse(information[6]), float.Parse(information[7]));
                    initialUserRotations.Add(information[0], g.transform.localRotation);
                    break;
                }
            }
        }

        ReadUserJoints();
        ReadClusterPoses();

        anotherUser.transform.position = new Vector3(0, -1, 4);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            flag = true;
            readMapping();
        }
        if (flag)
        {
            foreach (KeyValuePair<GameObject, GameObject> pair in mapping)
            {
                if (pair.Key.transform.name == "Dummy002")
                {
                    Quaternion temp = pair.Value.transform.rotation;
                    pair.Key.transform.rotation = Quaternion.Euler(-temp.eulerAngles.z, 0, 0) * Quaternion.Euler(0, 90, 0);
                }
                else if (pair.Key.transform.name == "Bone002" || pair.Key.transform.name == "Bone004" || pair.Key.transform.name == "Bone006")
                {
                    Quaternion temp = pair.Value.transform.localRotation * Quaternion.Inverse(initialUserRotations[pair.Value.transform.name]);
                    Quaternion initial = initialRotations[pair.Key.transform.name];
                    pair.Key.transform.localRotation = Quaternion.Euler(0, 0, temp.eulerAngles.z) * initial;
                }
                else if (pair.Key.transform.name == "Bone018")
                {
                    Quaternion temp = pair.Value.transform.localRotation * Quaternion.Inverse(initialUserRotations[pair.Value.transform.name]);
                    Quaternion initial = initialRotations[pair.Key.transform.name];
                    pair.Key.transform.localRotation = Quaternion.Euler(0, 0, -temp.eulerAngles.z) * initial;
                }
                else
                {
                    Quaternion temp = pair.Value.transform.localRotation * Quaternion.Inverse(initialUserRotations[pair.Value.transform.name]);
                    Quaternion initial = initialRotations[pair.Key.transform.name];
                    pair.Key.transform.localRotation = Quaternion.Euler(0, 0, 20 + temp.eulerAngles.x) * initial;
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.A) && clusterPoseCnt < 3)
        {
            text.text = clusterPoseCnt.ToString();
            clusterPoseRotations.Clear();
            poseDeviations.Clear();
            timer = 0f;
            recordTimer = Time.time;
            poseFlag = false;
            foreach (KeyValuePair<string, List<float>> pair in clusterPoses[clusterPoseCnt])
            {
                foreach (Transform g in anotherAvatar.transform.GetComponentsInChildren<Transform>())
                {
                    if (g.name == pair.Key)
                    {
                        g.localPosition = new Vector3(pair.Value[0], pair.Value[1], pair.Value[2]);
                        g.localRotation = new Quaternion(pair.Value[3], pair.Value[4], pair.Value[5], pair.Value[6]);
                        clusterPoseRotations.Add(g.name, g.localRotation);
                        break;
                    }
                }
            }
            clusterPoseCnt += 1;
        }
        if (!poseFlag && clusterPoseCnt > 0 && clusterPoseCnt < 4)
        {
            // after 1s, record the deviaiton of each joint (average)
            // record the timer
            bool tempFlag = true;
            float tDevia = 0f;
            foreach (Transform child in avatar.GetComponentsInChildren<Transform>())
            {
                if (controlledJoints.Contains(child.name))
                {
                    Quaternion a = clusterPoseRotations[child.name];
                    Quaternion b = child.localRotation;
                    float angle = 0f;
                    Vector3 axis = Vector3.zero;
                    (a * Quaternion.Inverse(b)).ToAngleAxis(out angle, out axis);
                    if (angle > 180)
                    {
                        angle -= 360;
                    }
                    angle = Mathf.Abs(angle);
                    tDevia += angle;
                }
            }
            if (tDevia / controlledJoints.Count < bestDeviation)
            {
                bestDeviation = tDevia / controlledJoints.Count;
            }
            if (tDevia / controlledJoints.Count > 0)
            {
                // text.text = child.name + " " + angle.ToString();
                tempFlag = false;
            }
            // text.text = duration.ToString() + " " + (tDevia / controlledJoints.Count).ToString();
            tDevia /= controlledJoints.Count;
            if (tempFlag)
            {
                poseDeviations.Add(tDevia);
                timer += Time.deltaTime;
                if (timer > 1f)
                {
                    float ttDevia = 0f;
                    foreach (float t in poseDeviations)
                    {
                        ttDevia += t;
                    }
                    ttDevia /= poseDeviations.Count;
                    float duration = Time.time - recordTimer;
                    writer.WriteLine(clusterPoseCnt + " " + ttDevia + " " + duration.ToString());
                    poseFlag = true;
                    timer = 0f;
                    poseDeviations.Clear();
                    text.text = "Complete";
                }
            }
            else
            {
                poseDeviations.Clear();
                timer = 0f;
            }
            if (Input.GetKeyDown(KeyCode.T))
            {
                float duration = Time.time - recordTimer;
                writer.WriteLine(clusterPoseCnt + " " + tDevia + " " + duration.ToString());
                poseFlag = true;
                timer = 0f;
                poseDeviations.Clear();
                text.text = "Complete";
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Z))
        {
            print(player.allMotions.Count);
            print(recorder.poses.Count);
            writer.WriteLine("Avatar");
            int tempCnt = 0;
            foreach (Dictionary<string, Quaternion> t in player.allMotions)
            {
                writer.WriteLine(tempCnt);
                tempCnt += 1;
                foreach (KeyValuePair<string, Quaternion> pair in t)
                {
                    if (controlledJoints.Contains(pair.Key))
                    {
                        writer.WriteLine(pair.Key + " " + ConvertQuaternionToString(pair.Value));
                    }
                }
            }
            writer.WriteLine("Users");
            tempCnt = 0;
            foreach (Dictionary<string, Quaternion> i in recorder.poses)
            {
                writer.WriteLine(tempCnt);
                tempCnt += 1;
                foreach (KeyValuePair<string, Quaternion> pair in i)
                {
                    if (controlledJoints.Contains(pair.Key))
                    {
                        writer.WriteLine(pair.Key + " " + ConvertQuaternionToString(pair.Value));
                    }
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        writer.Close();
    }
}
