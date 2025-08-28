using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using SkiaSharp;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

namespace McpClient.ViewModels;

internal class MonitorViewModel : ReactiveObject
{
    public ObservableCollection<ColumnSeries<int>> Series { get; set; } = new();

    public MonitorViewModel()
    {
        // see:
        // https://livecharts.dev/docs/avalonia/2.0.0-rc5.4/samples.axes.labelsFormat2
        LiveCharts.Configure(config => config.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('汉')));
    }

    public Axis[] XAxes { get; set; } =
    [
        new Axis
        {
            Labels = ["CPU", "3D", "Compute"],
            LabelsRotation = 0,
            //SeparatorsPaint = new SolidColorPaint(new SKColor(200, 200, 200)),
            SeparatorsAtCenter = false,
            //TicksPaint = new SolidColorPaint(new SKColor(35, 35, 35)),
            TicksAtCenter = true,
            // By default the axis tries to optimize the number of 
            // labels to fit the available space, 
            // when you need to force the axis to show all the labels then you must: 
            ForceStepToMin = true,
            MinStep = 1
        }
    ];

    public Axis[] YAxes { get; set; } =
    [
        new Axis
        {
            MinLimit = 0,
            MaxLimit = 100,
            // By default the axis tries to optimize the number of 
            // labels to fit the available space, 
            // when you need to force the axis to show all the labels then you must: 
            ForceStepToMin = true,
            MinStep = 20
        }
    ];
}
