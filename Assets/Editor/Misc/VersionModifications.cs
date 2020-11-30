using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Xml;
using System.IO;

public class VersionModifications : EditorWindow
{
    static VersionModifications instance = null;

    string assets1 = string.Empty;
    string assets2 = string.Empty;

    List<string> searchFolders = new List<string>();

    List<string> changeList = new List<string>();

    [MenuItem("Tools/其他/版本间差异对比")]
    public static void Check()
    {
        if (instance != null)
        {
            instance.Close();
            instance = null;
        }
        instance = GetWindow<VersionModifications>();
        instance.titleContent = new GUIContent("版本差异");
        instance.Show();
    }

    private void OnEnable()
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(Application.dataPath + "/Editor/Misc/VersionMod.xml");
        var mods = doc.SelectSingleNode("Mod").ChildNodes;
        for (int i = 0; i < mods.Count; ++i)
        {
            XmlNode node = mods[i];
            if (node == null)
            {
                continue;
            }
            XmlElement mod = node as XmlElement;
            if (mod != null && mod.Name.Equals("Folder"))
            {
                string folder = mod.GetAttribute("path");
                searchFolders.Add(folder);
            }
        }
        changeList.Add("Assets1\tAssets2");
    }

    private void OnGUI()
    {
        string txt = "检查以下文件夹内的变更：";
        for (int i = 0; i < searchFolders.Count; ++i)
        {
            string f = searchFolders[i];
            txt += $"\nAssets{f}";
        }
        txt += "\n\n以上内容请配置Assets/Editor/Misc/VersionMod.xml";
        EditorGUILayout.HelpBox(txt, MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("对比目录1（Assets）", GUILayout.Width(110.0f));
        EditorGUILayout.TextField(assets1);
        if (GUILayout.Button("选择"))
        {
            assets1 = EditorUtility.OpenFolderPanel("选择对比目录", Application.dataPath, "");
            if (!string.IsNullOrEmpty(assets1))
            {
                if (!assets1.EndsWith("Assets"))
                {
                    EditorUtility.DisplayDialog("错误", "请选择Assets目录", "确定");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("对比目录2（Assets）", GUILayout.Width(110.0f));
        EditorGUILayout.TextField(assets2);
        if (GUILayout.Button("选择"))
        {
            assets2 = EditorUtility.OpenFolderPanel("选择对比目录", Application.dataPath, "");
            if (!string.IsNullOrEmpty(assets2))
            {
                if (!assets2.EndsWith("Assets"))
                {
                    EditorUtility.DisplayDialog("错误", "请选择Assets目录", "确定");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("保存差异"))
        {
            string diffDir = EditorUtility.SaveFolderPanel("保存差异", Application.dataPath, "");
            if (string.IsNullOrEmpty(diffDir))
            {
                return;
            }
            CheckModify(diffDir);
        }
    }

    void CheckModify(string dstDirectory)
    {
        string dst = dstDirectory + "/Diff";

        for (int i = 0; i < searchFolders.Count; ++i)
        {
            string f = searchFolders[i];
            CheckModifyRecursion(assets1 + f, assets2 + f, dst);
        }

        string[] lines = changeList.ToArray();
        if (!Directory.Exists(dst))
        {
            Directory.CreateDirectory(dst);
        }
        string filePath = dst + "/change_list.txt";
        File.WriteAllLines(filePath, lines);

        if (EditorUtility.DisplayDialog("提升", "导出成功，是否打开文件夹", "确定", "取消"))
        {
            dst = dst.Replace("/", "\\");
            System.Diagnostics.Process.Start("explorer.exe", dst);
        }
    }

    void CheckModifyRecursion(string p1, string p2, string dst)
    {
        string[] files1 = Directory.GetFiles(p1);
        string[] files2 = Directory.GetFiles(p2);
        List<string> lsFiles2 = new List<string>(files2);
        int lastIndex2 = lsFiles2.Count - 1;
        List<string> lsFiles1 = new List<string>(files1);
        int lastIndex1 = lsFiles1.Count - 1;

        for (int i1 = lastIndex1; i1 >= 0; --i1)
        {
            string file1 = lsFiles1[i1];
            string rp1 = GetRelPath(file1);
            //Debug.Log("rp1: " + rp1);
            for (int i2 = lastIndex2; i2 >= 0; --i2)
            {
                string file2 = lsFiles2[i2];
                string rp2 = GetRelPath(file2);
                //Debug.Log("rp2: " +rp2);
                if (rp1 == rp2)
                {
                    ExchangePos(lsFiles1, i1, lastIndex1);
                    --lastIndex1;
                    ExchangePos(lsFiles2, i2, lastIndex2);
                    --lastIndex2;
                    break;
                }
            }
        }
        if (lsFiles2.Count - lastIndex2 != lsFiles1.Count - lastIndex1)
        {
            Debug.LogError("Exception Files Count!!!!! p1: " + p1 + ", p2: " + p2);
            return;
        }

        for (int i = 0; i <= lastIndex1; ++i)
        {
            string f = lsFiles1[i];
            string rp = GetRelPath(f);
            string dstFile = dst + "/Assets1/" + rp;

            //Debug.Log("dstFile: " + dstFile);

            string dir = Path.GetDirectoryName(dstFile);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            changeList.Add(f + "\t-");
            File.Copy(f, dstFile, true);
        }
        for (int i = 0; i <= lastIndex2; ++i)
        {
            string f = lsFiles2[i];
            string rp = GetRelPath(f);
            string dstFile = dst + "/Assets2/" + rp;

            //Debug.Log("dstFile: " + dstFile);

            string dir = Path.GetDirectoryName(dstFile);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            changeList.Add("-\t" + f);
            File.Copy(f, dstFile, true);
        }

        for (int i = lastIndex1 + 1; i < lsFiles1.Count; ++i)
        {
            string f1 = Path.GetFileName(lsFiles1[i]);
            byte[] data1 = File.ReadAllBytes(lsFiles1[i]);
            for (int j = lastIndex2 + 1; j < lsFiles2.Count; ++j)
            {
                string f2 = Path.GetFileName(lsFiles2[j]);
                byte[] data2 = File.ReadAllBytes(lsFiles2[j]);
                if (f1 == f2)
                {
                    if (!BytesCompare(data1, data2))
                    {
                        string chgListTxt = string.Empty;

                        string rp1 = GetRelPath(lsFiles1[i]);
                        string dstFile1 = dst + "/Assets1/" + rp1;
                        string dir1 = Path.GetDirectoryName(dstFile1);
                        if (!Directory.Exists(dir1))
                        {
                            Directory.CreateDirectory(dir1);
                        }
                        chgListTxt += lsFiles1[i];
                        File.Copy(lsFiles1[i], dstFile1, true);

                        chgListTxt += "\t";

                        string rp2 = GetRelPath(lsFiles2[j]);
                        string dstFile2 = dst + "/Assets2/" + rp2;
                        string dir2 = Path.GetDirectoryName(dstFile2);
                        if (!Directory.Exists(dir2))
                        {
                            Directory.CreateDirectory(dir2);
                        }
                        chgListTxt += lsFiles2[j];

                        changeList.Add(chgListTxt);
                        File.Copy(lsFiles2[j], dstFile2, true);
                    }
                    break;
                }
            }
        }

        string[] directies1 = Directory.GetDirectories(p1);
        string[] directies2 = Directory.GetDirectories(p2);
        List<string> lsDir2 = new List<string>(directies2);
        lastIndex2 = lsDir2.Count - 1;
        List<string> lsDir1 = new List<string>(directies1);
        lastIndex1 = lsDir1.Count - 1;

        for (int i1 = lastIndex1; i1 >= 0; --i1)
        {
            string file1 = lsDir1[i1];
            string rp1 = GetRelPath(file1);
            //Debug.Log("rp1: " + rp1);
            for (int i2 = lastIndex2; i2 >= 0; --i2)
            {
                string file2 = lsDir2[i2];
                string rp2 = GetRelPath(file2);
                //Debug.Log("rp2: " +rp2);
                if (rp1 == rp2)
                {
                    ExchangePos(lsDir1, i1, lastIndex1);
                    --lastIndex1;
                    ExchangePos(lsDir2, i2, lastIndex2);
                    --lastIndex2;
                    break;
                }
            }
        }
        if (lsDir2.Count - lastIndex2 != lsDir1.Count - lastIndex1)
        {
            Debug.LogError("Exception Directories Count!!!!! p1: " + p1 + ", p2: " + p2);
            return;
        }
        for (int i = 0; i <= lastIndex1; ++i)
        {
            string f = lsDir1[i];
            string rp = GetRelPath(f);
            string dstFile = dst + "/Assets1/" + rp;

            CopyDirectory(f, dstFile, 1);
        }
        for (int i = 0; i <= lastIndex2; ++i)
        {
            string f = lsDir2[i];
            string rp = GetRelPath(f);
            string dstFile = dst + "/Assets2/" + rp;

            CopyDirectory(f, dstFile, 2);
        }
        for (int i = lastIndex1 + 1; i < lsDir1.Count; ++i)
        {
            string d1 = lsDir1[i];
            string f1 = Path.GetFileName(d1);
            for (int j = lastIndex2 + 1; j < lsDir2.Count; ++j)
            {
                string d2 = lsDir2[j];
                string f2 = Path.GetFileName(d2);
                if (f1 == f2)
                {
                    CheckModifyRecursion(d1, d2, dst);
                    break;
                }
            }
        }
    }

    void ExchangePos<T>(List<T> ls, int srcIndex, int dstIndex)
    {
        T t = ls[srcIndex];
        ls[srcIndex] = ls[dstIndex];
        ls[dstIndex] = t;
    }

    string GetRelPath(string file)
    {
        string a = "Assets/";
        int index = file.IndexOf(a);
        string rp = file.Substring(index + a.Length);
        return rp;
    }

    void CopyDirectory(string src, string dst, int chgType)
    {
        if (!Directory.Exists(dst))
        {
            Directory.CreateDirectory(dst);
        }
        string[] files = Directory.GetFiles(src);
        for (int i = 0; i < files.Length; ++i)
        {
            string f = files[i];
            string fileName = Path.GetFileName(f);
            string dstFile = Path.Combine(dst, fileName);

            string chgListTxt = string.Empty;
            if (chgType == 1)
            {
                chgListTxt = f + "\t-";
            }
            else if (chgType == 2)
            {
                chgListTxt = "-\t" + f;
            }
            changeList.Add(chgListTxt);

            File.Copy(f, dstFile, true);
        }

        string[] directories = Directory.GetDirectories(src);
        for (int i = 0; i < directories.Length; ++i)
        {
            string f = directories[i];
            string fileName = Path.GetFileName(f);
            string dstFile = Path.Combine(dst, fileName);
            CopyDirectory(f, dstFile, chgType);
        }
    }

    bool BytesCompare(byte[] data1, byte[] data2)
    {
        if (data1.Length != data2.Length)
        {
            return false;
        }
        if (data1 == null || data2 == null)
        {
            return false;
        }
        for (int i = 0; i < data1.Length; ++i)
        {
            if (data1[i] != data2[i])
            {
                return false;
            }
        }
        return true;
    }
}