window.dailyVitalsWeightBmi = {
    initialize() {
        const panels = document.querySelectorAll('[data-bmi-panel]');

        panels.forEach((panel) => {
            const form = panel.closest('form');
            if (!form) {
                return;
            }

            const weightInput = form.querySelector('[data-bmi-weight-input]');
            const unitInput = form.querySelector('[data-bmi-unit-input]');
            const valueTarget = panel.querySelector('[data-bmi-value]');
            const categoryTarget = panel.querySelector('[data-bmi-category]');
            const heightFt = Number.parseFloat(panel.dataset.heightFt || '');

            if (!weightInput || !unitInput || !valueTarget || !categoryTarget || !Number.isFinite(heightFt) || heightFt <= 0) {
                return;
            }

            const update = () => {
                const enteredWeight = Number.parseFloat(weightInput.value || '');
                if (!Number.isFinite(enteredWeight) || enteredWeight <= 0) {
                    setBmi(panel, valueTarget, categoryTarget, '--', 'Enter weight');
                    return;
                }

                const weightLb = unitInput.value === 'kg'
                    ? enteredWeight * 2.2046226218
                    : enteredWeight;
                const heightM = heightFt * 0.3048;
                const weightKg = weightLb * 0.453592;
                const bmi = weightKg / (heightM * heightM);
                const bmiText = bmi.toFixed(1);

                setBmi(panel, valueTarget, categoryTarget, bmiText, getCategory(bmi));
            };

            weightInput.addEventListener('input', update);
            unitInput.addEventListener('change', update);
            update();
        });
    }
};

function setBmi(panel, valueTarget, categoryTarget, bmiText, category) {
    valueTarget.textContent = bmiText;
    categoryTarget.textContent = category;

    panel.classList.remove('bmi-panel--ok', 'bmi-panel--watch', 'bmi-panel--high');
    if (category === 'Normal') {
        panel.classList.add('bmi-panel--ok');
    } else if (category === 'Underweight' || category === 'Overweight') {
        panel.classList.add('bmi-panel--watch');
    } else if (category === 'Obese') {
        panel.classList.add('bmi-panel--high');
    }
}

function getCategory(bmi) {
    if (bmi < 18.5) {
        return 'Underweight';
    }

    if (bmi < 25) {
        return 'Normal';
    }

    if (bmi < 30) {
        return 'Overweight';
    }

    return 'Obese';
}

document.addEventListener('DOMContentLoaded', () => window.dailyVitalsWeightBmi.initialize());
document.addEventListener('enhancedload', () => window.dailyVitalsWeightBmi.initialize());
