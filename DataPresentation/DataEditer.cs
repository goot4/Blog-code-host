using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class DataEditer : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Window/DataEditer")]
    public static void ShowExample()
    {
        DataEditer wnd = GetWindow<DataEditer>();
        wnd.titleContent = new GUIContent("DataEditer");
    }

    private DataManager _dataManager;
    public void CreateGUI()
    {
        m_VisualTreeAsset.CloneTree(rootVisualElement);
        var listView = rootVisualElement.Q<MultiColumnListView>();

        // Get Data from dataManager
        _dataManager = new DataManager();
        var displayedList = new List<DataEntry>(_dataManager.DataList);
        // Set MultiColumnListView.itemsSource to populate the data in the list.
        listView.itemsSource = displayedList;

        // For each column, set Column.makeCell to initialize each cell in the column.
        // You can index the columns array with names or numerical indices.
        // 使用含ContextualMenu功能的Label，具体action为copy
        listView.columns["ID"].makeCell = CreateLabelWithContextualMenu;
            listView.columns["KIND"].makeCell = CreateLabelWithContextualMenu;
        listView.columns["TITLE"].makeCell = CreateLabelWithContextualMenu;
        listView.columns["TIME"].makeCell = CreateLabelWithContextualMenu;
        listView.columns["READ"].makeCell = () => new Toggle();

        // For each column, set Column.bindCell to bind an initialized cell to a data item.
        listView.columns["ID"].bindCell = (VisualElement element, int index) =>
            (element as Label).text = displayedList[index].id.ToString();
        listView.columns["KIND"].bindCell = (VisualElement element, int index) =>
            (element as Label).text = displayedList[index].kind.ToString();
        listView.columns["TITLE"].bindCell = (VisualElement element, int index) =>
            (element as Label).text = displayedList[index].title;
        listView.columns["TIME"].bindCell = (VisualElement element, int index) =>
            (element as Label).text = displayedList[index].timeString;
        listView.columns["READ"].bindCell = (VisualElement element, int index) =>
        {
            (element as Toggle).value = displayedList[index].read;
            // 在每个dataEntry的read的toggle的callback中同步更新datalist中对应的dataEntry
            // 注意：通过id字段寻找dataEntry，确保id字段unique
            (element as Toggle).RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                for (int i = 0; i < _dataManager.DataList.Count; i++)
                {
                    if (_dataManager.DataList[i].id == displayedList[index].id)
                    {
                        _dataManager.DataList[i] = new DataEntry(
                            displayedList[index].id,
                            displayedList[index].kind,
                            displayedList[index].title,
                            displayedList[index].time,
                            evt.newValue
                        );
                        break;
                    }
                }
            });
        };

        // 实现列的显示/隐藏功能
        // 更新：MultiColumn ListView 自带隐藏显示功能，在表头按右键可以出现相应的显示选项
        // 在每一列的是否显示的toggle的callback中将对应列的是否显示与toggle值同步
        rootVisualElement.Q<VisualElement>("DataPresentSettingMenu").Query<Toggle>().ForEach(
            elem => elem.RegisterCallback<ChangeEvent<bool>>(
                evt => listView.columns[elem.name].visible=evt.newValue));
        
        // 实现按指定列排序功能
        listView.sortingEnabled = true;
        listView.columnSortingChanged += () =>
        {
            switch (listView.sortedColumns.First().columnName)
            {
                case "ID":
                    displayedList.Sort((x, y) => (x.id - y.id));
                    break;
                case "KIND":
                    displayedList.Sort((x, y) => (x.kind - y.kind));
                    break;
                case "TITLE":
                    displayedList.Sort((x, y) => String.Compare(x.title,y.title));
                    break;
                case "TIME":
                    displayedList.Sort((x, y) => System.DateTime.Compare(x.time,y.time));
                    break;
                case "READ":
                    return;
            }
            if (listView.sortedColumns.First().direction == SortDirection.Descending)
            {
                displayedList.Reverse();
            }
            listView.RefreshItems();
        };
        
        // 实现数据筛选功能
        rootVisualElement.Q<EnumFlagsField>("KIND").RegisterCallback<ChangeEvent<Enum>>(evt =>
        {
            displayedList.Clear();
            foreach (var dataEntry in _dataManager.DataList)
            {
                if ((dataEntry.kind & (DataEntry.Kind)evt.newValue) > 0)
                {
                    displayedList.Add(new DataEntry(dataEntry));
                }
            }

            // 注意：筛选后的displayedList的Count改变，因此使用RefreashItems会出错，因此需要重新建立Listview
            listView.Rebuild();
        });
        
        // 为实现多行标记及复制功能添加2个button
        listView.selectionType = SelectionType.Multiple;
        // Read指令：分别更新dataList和displayedList中对应行
        rootVisualElement.Q<Button>("READ").RegisterCallback<MouseUpEvent>(evt =>
        {
            // --todo-- 可优化 O(n2) -> O(n) by using dictionary
            foreach (DataEntry item in listView.selectedItems)
            {
                for (int i = 0; i < _dataManager.DataList.Count; i++)
                {
                    if (_dataManager.DataList[i].id == item.id)
                    {
                        _dataManager.DataList[i] = new DataEntry(
                            item.id,
                            item.kind,
                            item.title,
                            item.time,
                            true
                        );
                        break;
                    }
                }
            }

            foreach (int index in listView.selectedIndices)
            {
                displayedList[index] = new DataEntry(
                    displayedList[index].id,
                    displayedList[index].kind,
                    displayedList[index].title,
                    displayedList[index].time,
                    true
                );
            }
            listView.RefreshItems();
        });
        
        rootVisualElement.Q<Button>("COPY").RegisterCallback<MouseUpEvent>(evt =>
        {
            var sb = new StringBuilder();
            foreach (DataEntry item in listView.selectedItems)
            {
                sb.Append(item.id.ToString() + ' ' + item.kind.ToString() + ' ' + item.title + ' ' + item.time+"    ");
            }

            GUIUtility.systemCopyBuffer = sb.ToString();
        });
    }

    // 关闭窗口时，将可能更新过的datalist用savedata保存到本地文件
    public void OnDestroy()
    {
        _dataManager.SaveData();
    }
    
    // 生成包含ContextualMenu功能的Label作为数据单元格
    private VisualElement CreateLabelWithContextualMenu()
    {
        var label = new Label();
        // The manipulator handles the right click and sends a ContextualMenuPopulateEvent to the target element.
        // The callback argument passed to the constructor is automatically registered on the target element.
        label.AddManipulator(new ContextualMenuManipulator((evt) =>
        {
            evt.menu.AppendAction("Copy", (x) => 
                GUIUtility.systemCopyBuffer=label.text, DropdownMenuAction.AlwaysEnabled);
        }));
        return label;
    }
    
}
