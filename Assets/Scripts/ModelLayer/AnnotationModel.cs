using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UniRx;
using System.IO;
using System.Linq;
using System;

public class AnnotationModel : MonoBehaviour
{
    List<LabelInfo?>[] LabelInfos;

    Subject<Texture2D> LoadImageSubject = new Subject<Texture2D>();
    Subject<string[]> LoadClassNamesSubject = new Subject<string[]>();
    Subject<(Vector2 Position, Vector2 Size)[][]> LoadLabelsSubject = new Subject<(Vector2 Position, Vector2 Size)[][]>();
    Subject<List<string>> LoadFilesSubject = new Subject<List<string>>();
    public IObservable<Texture2D> OnLoadImage => LoadImageSubject;
    public IObservable<string[]> OnLoadClassNames => LoadClassNamesSubject;
    public IObservable<(Vector2 Position, Vector2 Size)[][]> OnLoadLabels => LoadLabelsSubject;
    public IObservable<List<string>> OnLoadFiles => LoadFilesSubject;

    FileManager _FileManager;
    private void Awake()
    {
        _FileManager = new FileManager(Application.dataPath);
        _ActionLogger = new ActionLogger(this);
    }

    void AssignLabelInfo(int classId, int labelId, Vector2 position, Vector2 size)
    {
        var list = LabelInfos[classId];
        if (list.Count <= labelId)
            list.Add(new LabelInfo(position, size));
        else
            list[labelId] = new LabelInfo(position, size);
    }
    public void CreateLabelInfoWithLog(int classId, int labelId, Vector2 position, Vector2 size)
    {
        AssignLabelInfo(classId, labelId, position, size);
        SetLog_Create(classId, labelId, position, size);
    }

    public void ResizeLabelInfoWithLog(int classId, int labelId, Vector2 position, Vector2 size)
    {
        var info = (LabelInfo)LabelInfos[classId][labelId];
        AssignLabelInfo(classId, labelId, position, size);
        SetLog_Resize(classId, labelId, info.Position, info.Size, position, size);
    }

    (Vector2 Position, Vector2 Size) RemoveLabelInfo(int classId, int labelId)
    {
        var labelInfo = (LabelInfo)LabelInfos[classId][labelId];
        var tuple = (labelInfo.Position, labelInfo.Size);
        LabelInfos[classId][labelId] = null;
        return tuple;
    }
    public void RemoveLabelInfoWithLog(int classId, int labelId)
    {
        var tuple = RemoveLabelInfo(classId, labelId);
        SetLog_Remove(classId, labelId, tuple.Position, tuple.Size);
    }

    void ChangeClassLabelInfo(int classId, int labelId, int newClassId, int newLabelId)
    {
        var info = (LabelInfo)LabelInfos[classId][labelId];
        var pos = info.Position;
        var size = info.Size;
        RemoveLabelInfo(classId, labelId);
        AssignLabelInfo(newClassId, newLabelId, pos, size);
    }
    public void ChangeClassLabelInfoWithLog(int classId, int labelId, int newClassId, int newLabelId)
    {
        ChangeClassLabelInfo(classId, labelId, newClassId, newLabelId);
        SetLog_ClassChange(classId, labelId, newClassId, newLabelId);
    }


    public void LoadFileinfos()
    {
        LoadFilesSubject.OnNext(_FileManager.LoadFileInfos());
    }
    public void LoadClassNames()
    {
        var classNames = _FileManager.LoadClassNames();
        LabelInfos = Enumerable.Range(0, classNames.Length).Select(_ => new List<LabelInfo?>()).ToArray();
        LoadClassNamesSubject.OnNext(classNames);
    }

    public void Load(string fileName)
    {
        _ActionLogger.Clear();
        LoadImage(fileName);
        LoadLabels(fileName);
    }
    public void FirstLoad()
    {
        var fileName = _FileManager.FileInfos[0].Name;
        LoadImage(fileName);
        LoadLabels(fileName);
    }
    public void LoadImage(string fileName)
    {
        LoadImageSubject.OnNext(_FileManager.ReadTexture(fileName));
    }
    public void LoadLabels(string fileName)
    {
        _FileManager.LoadOutputAsLabelInfo(fileName, LabelInfos);
        LoadLabelsSubject.OnNext(
            LabelInfos
            .Select(list => list.Select(_ =>
            {
                var label = (LabelInfo)_;
                return (label.Position, label.Size);
            }).ToArray()
            ).ToArray());
    }
    public void SaveLabels(string fileName)
    {
        _FileManager.Save(fileName, LabelInfos);
    }

    public void Undo() => _ActionLogger.Undo();
    public void Redo() => _ActionLogger.Redo();

    class FileManager
    {
        string[] ClassNames;
        public FileInfo[] FileInfos;
        Dictionary<string, int> ClassNameDictionary = new Dictionary<string, int>();
        Vector2 ImageCenter;

        static string[] SupportedExtension =
        {
            "*.png",
            "*.jpg",
            "*.jpeg"
        };

        string RootPath;
        string ImagePath;
        string OutputPath;
        string SettingPath;

        public FileManager(string dataPath)
        {
            Init(dataPath);
        }

        void Init(string dataPath)
        {
            var sb = new StringBuilder();
            sb.Append(dataPath);
            if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                // MacOSでビルドした場合のパス(.appファイルの同階層のパス)
                sb.Append("/../../");
            }
            else if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                // Windowsでビルドした場合のパス(.exeファイルの同階層のパス)
                sb.Append("/../");
            }
            else
            {
                // それ以外の場合(Assets/Resourcesフォルダのパス)
                sb.Append("/Resources/");
            }
            RootPath = sb.ToString();
            ImagePath = RootPath + "Image/";
            OutputPath = RootPath + "Output/";
            SettingPath = RootPath + "Setting/";
        }

        public string[] LoadClassNames()
        {
            var path = SettingPath + "Class.txt";
            Debug.Log($"{path}からClassデータを読み込み。");
            if (File.Exists(path))
            {
                ClassNames = File.ReadAllText(path).Split(","[0]);
                ClassNameDictionary = new Dictionary<string, int>();
                for (var i = ClassNames.Length - 1; i >= 0; i--)
                    ClassNameDictionary.Add(ClassNames[i], i);
                return ClassNames;
            }
            else
            {
                Debug.LogError("Classデータが存在しません。");
                return null;
            }
        }
        public void LoadOutputAsLabelInfo(string fileName, List<LabelInfo?>[] labelInfos)
        {
            foreach (var list in labelInfos)
                list.Clear();

            var sb = new StringBuilder();
            sb.Append(OutputPath);
            sb.Append(fileName);
            sb.Append(".csv");
            var path = sb.ToString();
            if (!File.Exists(path))
                return;
            Debug.Log($"{path}からラベルデータを読み込み。");
            var stringsList = File.ReadLines(path)
                .Select(line => line.Split(","[0]))
                .Where(list => list.Length == 5);
            foreach (var strings in stringsList)
            {
                labelInfos[ClassNameDictionary[strings[0]]].Add(
                     new LabelInfo(
                         int.Parse(strings[1]),
                         int.Parse(strings[2]),
                         int.Parse(strings[3]),
                         int.Parse(strings[4]),
                         ImageCenter));
            }

        }
        public void Save(string fileName, List<LabelInfo?>[] labels)
        {
            var text = ConvertToCsv(labels);

            StreamWriter sw;
            var filePath = new StringBuilder();
            filePath.Append(OutputPath);
            filePath.Append(fileName);
            filePath.Append(".csv");
            var path = filePath.ToString();
            var fileInfo = new FileInfo(path);

            RemoveFile(path);

            sw = fileInfo.AppendText();
            sw.WriteLine(text);
            sw.Flush();
            sw.Close();
            Debug.Log(fileName + "を保存しました。");
        }
        public Texture2D ReadTexture(string fileName)
        {
            var path = ImagePath + fileName;
            Debug.Log($"{path}を読み込み");
            byte[] readBinary = ReadFile(path);

            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(readBinary);
            ImageCenter = new Vector2(texture.width, texture.height) / 2;

            return texture;
        }
        byte[] ReadFile(string path)
        {
            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            BinaryReader bin = new BinaryReader(fileStream);
            byte[] values = bin.ReadBytes((int)bin.BaseStream.Length);
            bin.Close();
            return values;
        }
        string ConvertToCsv(List<LabelInfo?>[] labels)
        {
            if (labels == null)
                return "";
            var outputs = labels
                .Zip(ClassNames, (list, name) => (list, name))
                .SelectMany(tuple =>
                    tuple.Item1
                    .Where(_ => _ != null)
                    .Select(info => new Output(tuple.Item2, (LabelInfo)info, ImageCenter).ToCSV()))
                .ToArray();
            return string.Join("\n", outputs);

        }
        bool RemoveFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            return false;
        }
        public List<string> LoadFileInfos()
        {
            var list = new List<FileInfo>();
            DirectoryInfo dir = new DirectoryInfo(ImagePath);
            foreach (var ext in SupportedExtension)
            {
                list.AddRange(dir.GetFiles(ext));
            }
            FileInfos = list.ToArray();
            return list.Select(info => info.Name).ToList();
        }
    }

    #region ActionLogger

    ActionLogger _ActionLogger;

    public IObservable<int> OnUndoCountChanged => _ActionLogger.OnUndoCountChanged;
    public IObservable<int> OnRedoCountChanged => _ActionLogger.OnRedoCountChanged;

    Subject<(int ClassId, int LabelId, Vector2 Position, Vector2 Size)> CreateSubject_Log = new Subject<(int ClassId, int LabelId, Vector2 Position, Vector2 Size)>();
    Subject<(int ClassId, int LabelId, Vector2 Position, Vector2 SIze)> ResizeSubject_Log = new Subject<(int ClassId, int LabelId, Vector2 Position, Vector2 SIze)>();
    Subject<(int ClassId, int LabelId)> RemoveSubject_Log = new Subject<(int ClassId, int LabelId)>();
    Subject<(int ClassId, int LabelId, int NewClassId, int NewLabelId)> ChangeClassSubject_Log = new Subject<(int ClassId, int LabelId, int NewClassId, int NewLabelId)>();
    public IObservable<(int ClassId, int LabelId, Vector2 Position, Vector2 Size)> OnCreated_Log => CreateSubject_Log;
    public IObservable<(int ClassId, int LabelId, Vector2 Position, Vector2 Size)> OnResized_Log => ResizeSubject_Log;
    public IObservable<(int ClassId, int LabelId)> OnRemoved_Log => RemoveSubject_Log;
    public IObservable<(int ClassId, int LabelId, int NewClassId, int NewLabelId)> OnChangedClass_Log => ChangeClassSubject_Log;

    public void CreateLabelInfo_Log(int classId, int labelId, Vector2 position, Vector2 size)
    {
        AssignLabelInfo(classId, labelId, position, size);
        CreateSubject_Log.OnNext((classId, labelId, position, size));
    }
    public void ResizeLabelInfo_Log(int classId, int labelId, Vector2 position, Vector2 size)
    {
        AssignLabelInfo(classId, labelId, position, size);
        ResizeSubject_Log.OnNext((classId, labelId, position, size));
    }
    public void RemoveLabelInfo_Log(int classId, int labelId)
    {
        RemoveLabelInfo(classId, labelId);
        RemoveSubject_Log.OnNext((classId, labelId));
    }
    public void ChangeClassLabelInfo_Log(int classId, int labelId, int newClassId, int newLabelId)
    {
        ChangeClassLabelInfo(classId, labelId, newClassId, newLabelId);
        ChangeClassSubject_Log.OnNext((classId, labelId, newClassId, newLabelId));
    }

    void SetLog_Create(int classId, int labelId, Vector2 position, Vector2 size) => _ActionLogger.AddUndoWithClear(new ActionLogger.CreateLog(classId, labelId, position, size));
    void SetLog_Resize(int classId, int labelId, Vector2 position, Vector2 size, Vector2 newPosition, Vector2 newSize) => _ActionLogger.AddUndoWithClear(new ActionLogger.ResizeLog(classId, labelId, position, size, newPosition, newSize));
    void SetLog_Remove(int classId, int labelId, Vector2 position, Vector2 size) => _ActionLogger.AddUndoWithClear(new ActionLogger.RemoveLog(classId, labelId, position, size));
    void SetLog_ClassChange(int classId, int labelId, int newClassId, int newLabelId) => _ActionLogger.AddUndoWithClear(new ActionLogger.ChangeClassLog(classId, labelId, newClassId, newLabelId));

    class ActionLogger
    {
        AnnotationModel _AnnotationModel;

        const int maxLength = 20;
        ReactiveCollection<ActionLogInterface> UndoLogs = new ReactiveCollection<ActionLogInterface>();
        ReactiveCollection<ActionLogInterface> RedoLogs = new ReactiveCollection<ActionLogInterface>();

        public IObservable<int> OnUndoCountChanged => UndoLogs.ObserveCountChanged();
        public IObservable<int> OnRedoCountChanged => RedoLogs.ObserveCountChanged();

        public ActionLogger(AnnotationModel model)
        {
            _AnnotationModel = model;
        }

        public void Undo()
        {
            var cnt = UndoLogs.Count;
            var redo = UndoLogs[cnt - 1].Undo(_AnnotationModel);
            UndoLogs.RemoveAt(cnt - 1);
            AddRedo(redo);
        }
        public void Redo()
        {
            var cnt = RedoLogs.Count;
            var undo = RedoLogs[cnt - 1].Redo(_AnnotationModel);
            RedoLogs.RemoveAt(cnt - 1);
            AddUndo(undo);
        }
        public void AddUndo(ActionLogInterface action)
        {
            UndoLogs.Add(action);
            if (UndoLogs.Count > maxLength)
                UndoLogs.RemoveAt(0);
        }
        public void AddUndoWithClear(ActionLogInterface action)
        {
            ClearRedo();
            AddUndo(action);
        }
        void AddRedo(ActionLogInterface action)
        {
            RedoLogs.Add(action);
            if (RedoLogs.Count > maxLength)
                RedoLogs.RemoveAt(0);
        }

        public void Clear()
        {
            UndoLogs.Clear();
            RedoLogs.Clear();
        }
        public void ClearRedo()
        {
            RedoLogs.Clear();
        }

        public struct CreateLog : ActionLogInterface
        {
            int ClassId;
            int LabelId;
            Vector2 Positon;
            Vector2 Size;

            public ActionLogInterface Undo(AnnotationModel model)
            {
                model.RemoveLabelInfo_Log(ClassId, LabelId);
                return new CreateLog(ClassId, LabelId, Positon, Size);
            }

            public ActionLogInterface Redo(AnnotationModel model)
            {
                model.CreateLabelInfo_Log(ClassId, LabelId, Positon, Size);
                return new CreateLog(ClassId, LabelId, Positon, Size);

            }
            public CreateLog(int classId, int labelId, Vector2 pos, Vector2 size)
            {
                ClassId = classId;
                LabelId = labelId;
                Positon = pos;
                Size = size;
            }
        }
        public struct RemoveLog : ActionLogInterface
        {
            int ClassId;
            int LabelId;
            Vector2 Positon;
            Vector2 Size;

            public ActionLogInterface Undo(AnnotationModel model)
            {
                model.CreateLabelInfo_Log(ClassId, LabelId, Positon, Size);
                return new RemoveLog(ClassId, LabelId, Positon, Size);
            }
            public ActionLogInterface Redo(AnnotationModel model)
            {
                model.RemoveLabelInfo_Log(ClassId, LabelId);
                return new RemoveLog(ClassId, LabelId, Positon, Size);
            }
            public RemoveLog(int classId, int labelId, Vector2 pos, Vector2 size)
            {
                ClassId = classId;
                LabelId = labelId;
                Positon = pos;
                Size = size;
            }
        }
        public struct ChangeClassLog : ActionLogInterface
        {
            int PreviousClassId;
            int PreviousLabelId;

            int CurrentClassId;
            int CurrentLabelId;

            public ActionLogInterface Undo(AnnotationModel model)
            {
                model.ChangeClassLabelInfo_Log(CurrentClassId, CurrentLabelId, PreviousClassId, PreviousLabelId);
                return this;
            }
            public ActionLogInterface Redo(AnnotationModel model)
            {
                model.ChangeClassLabelInfo_Log(PreviousClassId, PreviousLabelId, CurrentClassId, CurrentLabelId);
                return this;
            }
            public ChangeClassLog(int classId, int labelId, int newClassId, int newLabelId)
            {
                PreviousClassId = classId;
                PreviousLabelId = labelId;
                CurrentClassId = newClassId;
                CurrentLabelId = newLabelId;
            }
        }
        public struct ResizeLog : ActionLogInterface
        {
            int ClassId;
            int LabelId;

            Vector2 PreviousPosition;
            Vector2 PreviousSize;

            Vector2 CurrentPosition;
            Vector2 CurrentSize;

            public ActionLogInterface Undo(AnnotationModel model)
            {
                model.ResizeLabelInfo_Log(ClassId, LabelId, PreviousPosition, PreviousSize);
                return new ResizeLog(ClassId, LabelId, PreviousPosition, PreviousSize, CurrentPosition, CurrentSize);
            }
            public ActionLogInterface Redo(AnnotationModel model)
            {
                model.ResizeLabelInfo_Log(ClassId, LabelId, CurrentPosition, CurrentSize);
                return new ResizeLog(ClassId, LabelId, PreviousPosition, PreviousSize, CurrentPosition, CurrentSize);
            }
            public ResizeLog(int classId, int labelId, Vector2 previousPosition, Vector2 previousSize, Vector2 currentPosition, Vector2 currentSize)
            {
                ClassId = classId;
                LabelId = labelId;
                PreviousPosition = previousPosition;
                PreviousSize = previousSize;
                CurrentPosition = currentPosition;
                CurrentSize = currentSize;
            }
        }

        public interface ActionLogInterface
        {
            ActionLogInterface Redo(AnnotationModel model);
            ActionLogInterface Undo(AnnotationModel model);
        }
    }
    #endregion
    readonly struct LabelInfo
    {

        public readonly Vector2 Position;
        public readonly Vector2 Size;

        public LabelInfo(Vector2 position, Vector2 size)
        {
            Position = position;
            Size = size;
        }
        public LabelInfo(int minX, int minY, int maxX, int maxY, Vector2 center)
        {
            Position = new Vector2(minX - center.x, center.y - maxY);
            Size = new Vector2(maxX - minX, maxY - minY);
        }
    }
    readonly struct Output
    {
        public readonly string ClassName;
        public readonly int MinX;
        public readonly int MaxX;
        public readonly int MinY;
        public readonly int MaxY;

        public Output(string className, LabelInfo labelInfo, Vector2 center)
        {
            ClassName = className;

            var labelSize = labelInfo.Size;
            var position = labelInfo.Position;

            var x = center.x + position.x;
            var Y = center.y - position.y;
            var X = x + labelSize.x;
            var y = Y - labelSize.y;

            MinX = (int)x;
            MaxX = (int)X;
            MinY = (int)y;
            MaxY = (int)Y;
        }
        public Output(string name, int x, int y, int X, int Y)
        {
            ClassName = name;
            MinX = x;
            MinY = y;
            MaxX = X;
            MaxY = Y;
        }

        public string ToCSV()
        {
            var sb = new StringBuilder();
            sb.Append(ClassName);
            sb.Append(",");
            sb.Append(MinX);
            sb.Append(",");
            sb.Append(MinY);
            sb.Append(",");
            sb.Append(MaxX);
            sb.Append(",");
            sb.Append(MaxY);

            return sb.ToString();
        }
        override public string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(ClassName);
            sb.Append(":min{x:");
            sb.Append(MinX);
            sb.Append(",y:");
            sb.Append(MinY);
            sb.Append("}-max{x:");
            sb.Append(MaxX);
            sb.Append(",y:");
            sb.Append(MaxY);
            sb.Append("}");
            return sb.ToString();
        }
    }

#if DEBUG
    public void DebugLabel()
    {
        foreach (var info in LabelInfos.SelectMany(_ => _).Where(i => i != null))
        {
            var label = (LabelInfo)info;
            Debug.Log($"P:{label.Position},S:{label.Size}");
        }
    }
#endif
}
