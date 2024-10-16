using System;
using System.ComponentModel;

public class ObservableString : INotifyPropertyChanged
{
    private string _value;

    public string Value
    {
        get { return _value; }
        set
        {
            if (_value != value)
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
                OnValueChanged(); // 当值变化时调用该函数
            }
        }
    }

    public bool IsChecked { get; internal set; }

    public event EventHandler ValueChanged; // 定义一个事件

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // 在这里定义值改变时要调用的函数
    private void OnValueChanged()
    {
        ValueChanged?.Invoke(this, EventArgs.Empty); // 引发事件
    }
}
