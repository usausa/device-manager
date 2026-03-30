// ReSharper disable UseOfImplicitGlobalInFunctionScope
window.initControl = () => {
    var g = new RadialGauge({
        renderTo: "gauge",
        width: 196,
        height: 196,
        units: "V",
        value: 100,
        minValue: 96,
        maxValue: 104,
        majorTicks: [96, 98, 100, 102, 104],
                highlights: false,
                valueBox: false
    }).draw();

    //setInterval(function () {
    //    g.value = 99.5 + Math.random() * 1;
    //}, 2000);
}
// ReSharper restore UseOfImplicitGlobalInFunctionScope
