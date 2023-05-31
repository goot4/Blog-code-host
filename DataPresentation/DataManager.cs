using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class DataManager
{
    public List<DataEntry> DataList { get; private set; }

    private DataConvertor _dataConvertor;
    
    public DataManager()
    {
        _dataConvertor = new DataConvertor();
        DataList = _dataConvertor.LoadData() ?? GenerateRandomData(50);
    }

    // public void SortBy(string column,SortDirection direction)
    // {
    //     switch (column)
    //     {
    //         case "ID":
    //             DataList.Sort((x, y) => (x.id - y.id));
    //             break;
    //         case "KIND":
    //             DataList.Sort((x, y) => (x.kind - y.kind));
    //             break;
    //         case "TITLE":
    //             DataList.Sort((x, y) => String.Compare(x.title,y.title));
    //             break;
    //         case "TIME":
    //             DataList.Sort((x, y) => System.DateTime.Compare(x.time,y.time));
    //             break;
    //         case "READ":
    //             return;
    //     }
    //     if (direction == SortDirection.Descending)
    //     {
    //         DataList.Reverse();
    //     }
    // }
    private List<DataEntry> GenerateRandomData(int n)
    {
        var dataList = new List<DataEntry>();
        for (int i = 0; i < n; i++)
        {
            var kindValues = Enum.GetValues(typeof(DataEntry.Kind));
            dataList.Add(new DataEntry(
                Random.Range(0,500),
                (DataEntry.Kind)kindValues.GetValue(Random.Range(0,kindValues.Length)),
                "title"+(i+1),
                System.DateTime.Now.AddDays(Random.Range(-30,30)),
                false
            ));
        }
        return dataList;
    }

    public void SaveData() => _dataConvertor.SaveData(new DataEntryWrapper(DataList));
}
