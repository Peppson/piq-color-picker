using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace ColorPicker.Converters;

public class BoolToCursorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && b ? Cursors.Arrow : Cursors.Hand;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
