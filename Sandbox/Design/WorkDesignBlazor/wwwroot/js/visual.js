// ReSharper disable UseOfImplicitGlobalInFunctionScope
window.initVisual = () => {
    let color = {
        lightGreen: "#00d7af",
        lightWhite: "#f8f8ff",
        lightGrey: "#e0e0e0",
        lightBlack: "#343a42",
        black: "#000000",
        white: "#ffffff",
        red: "rgba(255, 99, 132)",
        green: "rgba(75, 192, 192)",
        blue: "rgba(54, 162, 235)",
        yellow: "rgba(255, 205, 86)",
        transparent: "rgba(255, 255, 255, 0)"
    };

    function getRandomInt(max) {
        return Math.floor(Math.random() * (max + 1));
    }

    // Clock
    // ReSharper disable once UnusedLocals
    var clock = new zeu.DigitalClock("digital-clock", {
        numberColor: color.blue,
        dashColor: color.white,
        hourOffset: 5
    });

    // Speed
    var roundFan1 = new zeu.RoundFan("round-fan1", {
        fanColor: color.green,
        center: {
            color: color.white,
            bgColor: color.green
        }
    });

    var roundFan2 = new zeu.RoundFan("round-fan2", {
        fanColor: color.blue,
        center: {
            color: color.white,
            bgColor: color.blue
        }
    });

    setInterval(function () {
        roundFan1.speed = getRandomInt(10);
        roundFan2.speed = getRandomInt(10);
    }, 3000);

    // Bar
    var barMeter1 = new zeu.BarMeter("bar-meter1", {
        min: 0,
        max: 100,
        dashColor: color.lightGrey
    });
    var barMeter2 = new zeu.BarMeter("bar-meter2", {
        min: 0,
        max: 100,
        dashColor: color.lightGrey
    });
    var barMeter3 = new zeu.BarMeter("bar-meter3", {
        min: 0,
        max: 100,
        dashColor: color.lightGrey
    });
    var barMeter4 = new zeu.BarMeter("bar-meter4", {
        min: 0,
        max: 100,
        dashColor: color.lightGrey
    });

    function updateMeter() {
        const value1 = getRandomInt(100);
        barMeter1.value = value1;
        if (value1 <= 40) {
            barMeter1.speed = 5;
            barMeter1.barColor = color.green;
        } else if (value1 >= 80) {
            barMeter1.speed = 25;
            barMeter1.barColor = color.red;
        } else {
            barMeter1.speed = 10;
            barMeter1.barColor = color.yellow;
        }
        const value2 = getRandomInt(100);
        barMeter2.value = value2;
        if (value2 <= 40) {
            barMeter2.speed = 5;
            barMeter2.barColor = color.green;
        } else if (value2 >= 80) {
            barMeter2.speed = 25;
            barMeter2.barColor = color.red;
        } else {
            barMeter2.speed = 10;
            barMeter2.barColor = color.yellow;
        }
        const value3 = getRandomInt(75);
        barMeter3.value = value3;
        if (value3 <= 40) {
            barMeter3.speed = 5;
            barMeter3.barColor = color.green;
        } else if (value3 >= 80) {
            barMeter3.speed = 25;
            barMeter3.barColor = color.red;
        } else {
            barMeter3.speed = 10;
            barMeter3.barColor = color.yellow;
        }
        const value4 = getRandomInt(75) + 25;
        barMeter4.value = value4;
        if (value4 <= 40) {
            barMeter4.speed = 5;
            barMeter4.barColor = color.green;
        } else if (value4 >= 80) {
            barMeter4.speed = 25;
            barMeter4.barColor = color.red;
        } else {
            barMeter4.speed = 10;
            barMeter4.barColor = color.yellow;
        }
    }

    setInterval(function () {
        updateMeter();
    }, 3000);

    updateMeter();

    // Grid
    var hexGrid = new zeu.HexGrid("hex-grid", {
        space: 4,
        radius: 24,
        border: 1
    });

    var i;
    for (i = 0; i < 4; i++) {
        hexGrid.saveHex({
            id: `node-${i + 1}`,
            x: 0,
            y: i,
            bgColor: color.green,
            borderColor: color.white,
            text: {
                value: `Node-${i + 1}`,
                color: color.white,
                font: "10px Arial",
                xOffset: 0,
                yOffset: 5
            }
        });
    }
    for (i = 0; i < 3; i++) {
        hexGrid.saveHex({
            id: `node-${i + 5}`,
            x: 1,
            y: i,
            bgColor: color.green,
            borderColor: color.white,
            text: {
                value: `Node-${i + 5}`,
                color: color.white,
                font: "10px Arial",
                xOffset: 0,
                yOffset: 5
            }
        });
    }
    for (i = 0; i < 4; i++) {
        hexGrid.saveHex({
            id: `node-${i + 8}`,
            x: 2,
            y: i,
            bgColor: color.green,
            borderColor: color.white,
            text: {
                value: `Node-${i + 8}`,
                color: color.white,
                font: "10px Arial",
                xOffset: 0,
                yOffset: 5
            }
        });
    }

    hexGrid.blinkOn({
        id: "node-3",
        text: {
            value: "WARN",
            color: color.white
        },
        bgColor: color.red,
        borderColor: color.white,
        interval: 1000
    });


    // Meter
    var textMeter1 = new zeu.TextMeter("text-meter1", {
        viewWidth: 400,
        arrowColor: color.green,
        displayValue: "MIDDLE",
        marker: {
            bgColor: color.black,
            fontColor: color.green
        },
        bar: {
            speed: 15,
            fillColor: color.green,
            bgColor: color.black,
            borderColor: color.black
        }
    });
    var textMeter2 = new zeu.TextMeter("text-meter2", {
        viewWidth: 400,
        arrowColor: color.green,
        displayValue: "MIDDLE",
        marker: {
            bgColor: color.black,
            fontColor: color.green
        },
        bar: {
            speed: 15,
            fillColor: color.green,
            bgColor: color.black,
            borderColor: color.black
        }
    });

    setInterval(function () {
        const value1 = getRandomInt(100);
        textMeter1.value = value1;
        if (value1 <= 30) {
            textMeter1.displayValue = "LOW";
            textMeter1.fillColor = color.green;
            textMeter1.arrowColor = color.green;
            textMeter1.markerFontColor = color.green;
        } else if (value1 >= 60) {
            textMeter1.displayValue = "HIGH";
            textMeter1.fillColor = color.red;
            textMeter1.arrowColor = color.red;
            textMeter1.markerFontColor = color.red;
        } else {
            textMeter1.displayValue = "MIDDLE";
            textMeter1.fillColor = color.yellow;
            textMeter1.arrowColor = color.yellow;
            textMeter1.markerFontColor = color.yellow;
        }
        const value2 = getRandomInt(100);
        textMeter2.value = value2;
        if (value2 <= 30) {
            textMeter2.displayValue = "LOW";
            textMeter2.fillColor = color.green;
            textMeter2.arrowColor = color.green;
            textMeter2.markerFontColor = color.green;
        } else if (value2 >= 60) {
            textMeter2.displayValue = "HIGH";
            textMeter2.fillColor = color.red;
            textMeter2.arrowColor = color.red;
            textMeter2.markerFontColor = color.red;
        } else {
            textMeter2.displayValue = "MIDDLE";
            textMeter2.fillColor = color.yellow;
            textMeter2.arrowColor = color.yellow;
            textMeter2.markerFontColor = color.yellow;
        }
    }, 2000);
}
// ReSharper restore UseOfImplicitGlobalInFunctionScope
