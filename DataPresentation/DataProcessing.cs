using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

[Serializable]
public struct DataEntry {
    public int id;
    // 更改了题目中kind中成员的顺序，以便排序方便
    // 将Kind的底层值改成flag，使其兼容EnumFlagsField
    [Flags]
    public enum Kind { Trash=1<<0, Normal=1<<1, Important=1<<2 } 
    public Kind kind;
    public string title;
    public System.DateTime time;
    public string timeString;  // Use string type to save time to JSON, update when time get updated
    public bool read;
    
    public DataEntry(int id, Kind kind, string title, DateTime time, bool read)
    {
        this.id = id;
        this.kind = kind;
        this.title = title;
        this.time = time;
        this.timeString = time.ToString();
        this.read = read;
    }

    public DataEntry(DataEntry dataEntry)
    {
        this.id = dataEntry.id;
        this.kind = dataEntry.kind;
        this.title = dataEntry.title;
        this.time = dataEntry.time;
        this.timeString = dataEntry.time.ToString();
        this.read = dataEntry.read;
    }
}

[Serializable]
public class DataEntryWrapper
{
    public List<DataEntry> DataEntries;

    public DataEntryWrapper(List<DataEntry> dataEntries)
    {
        DataEntries = dataEntries;
    }
}

public class DataConvertor
{
    private string dataFileDirPath;
    private string dataFileName;

    public DataConvertor()
    {
        dataFileDirPath = Application.persistentDataPath;
        dataFileName = "data.json";
    }
    
    public List<DataEntry> LoadData()
    {
        var fullPath = Path.Combine(dataFileDirPath, dataFileName);
        string jsonData;
        if (!File.Exists(fullPath)) return null;
        
        using (FileStream stream = new FileStream(fullPath, FileMode.Open))
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                jsonData = reader.ReadToEnd();
            }
        }

        var dataList = JsonUtility.FromJson<DataEntryWrapper>(jsonData).DataEntries;
        
        // loadedData缺少time字段，需要从timeString字段转换而来，又因为数据为value type，无法修改结构体的field，需要整体替换
        for (int i = 0; i < dataList.Count; i++)
        {
            dataList[i] = new DataEntry(
                dataList[i].id,
                dataList[i].kind,
                dataList[i].title,
                System.DateTime.Parse(dataList[i].timeString),
                dataList[i].read
                );
        }
        return dataList;
    }

    // 将数据转换为存储格式，并输出到对于文件夹
    public void SaveData(DataEntryWrapper dataWrapper)
    {
        var fullPath = Path.Combine(dataFileDirPath, dataFileName);
        
        // 创建目录如果目录不存在
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        
        // serialize the data to Json
        var jsonData = JsonUtility.ToJson(dataWrapper, true);

        // using these objects in a using statement so that the unmanaged resources are correctly disposed.
        using (FileStream stream = new FileStream(fullPath, FileMode.Create))
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(jsonData);
            }
        }
    }
}
