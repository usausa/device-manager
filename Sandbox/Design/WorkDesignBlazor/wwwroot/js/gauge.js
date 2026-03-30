// ReSharper disable UseOfImplicitGlobalInFunctionScope
window.initGauge = () => {
    var rg1 = new RadialGauge({
        renderTo: "radial-gauge1",
        width: 300,
        height: 300,
        barWidth: 5,
        barShadow: 5,
        colorBarProgress: "rgba(50, 200, 50, .75)"
    }).draw();

    var rg2 = new RadialGauge({
        renderTo: "radial-gauge2",
        width: 300,
        height: 300,
        units: "RPM",
        startAngle: 45,
        ticksAngle: 225,
        valueBox: true,
        minValue: 0,
        maxValue: 100,

        minorTicks: 2,
        strokeTicks: true,
        highlights: [
            {
                "from": 0,
                "to": 60,
                "color": "rgba(75, 192, 192, .2)"
            },
            {
                "from": 60,
                "to": 80,
                "color": "rgba(255, 205, 86, .75)"
            },
            {
                "from": 80,
                "to": 100,
                "color": "rgba(255, 99, 132, .75)"
            }
        ],
        colorPlate: "#fff",
        borderShadowWidth: 0,
        borders: false,
        needleType: "arrow",
        needleWidth: 5,
        needleCircleSize: 2,
        needleCircleOuter: true,
        needleCircleInner: false,
        animationDuration: 500,
        animationRule: "linear"
    }).draw();

    var lg = new LinearGauge({
        renderTo: "linear-gauge",
        width: 100,
        height: 300,
        borderRadius: 0,
        borders: 0,
        barBeginCircle: 15,
        minorTicks: 5,
        minValue: 75,
        maxValue: 100,
        title: "°C",
        majorTicks: [75, 80, 85, 90, 95, 100],
        barWidth: 5,
        highlights: false,
        colorValueBoxShadow: false,
        valueBoxStroke: 0,
        colorValueBoxBackground: false,
        valueInt: 2,
        valueDec: 1
    }).draw();

    setInterval(function () {
        rg1.value = rg1.options.minValue + Math.random() * (rg1.options.maxValue - rg1.options.minValue);
        rg2.value = rg2.options.minValue + Math.random() * (rg2.options.maxValue - rg2.options.minValue);
        lg.value = lg.options.minValue + Math.random() * (lg.options.maxValue - lg.options.minValue);
    }, 1000);
}
// ReSharper restore UseOfImplicitGlobalInFunctionScope
