window.dailyVitalsExerciseOther = {
    initialize() {
        document.querySelectorAll('[data-other-exercise-select]').forEach((select) => {
            const form = select.closest('form');
            if (!form) {
                return;
            }

            const field = form.querySelector('[data-other-exercise-field]');
            const input = form.querySelector('[data-other-exercise-input]');
            const otherValue = select.dataset.otherExerciseValue;
            if (!field || !input || !otherValue) {
                return;
            }

            const update = () => {
                const isOther = select.value === otherValue;
                field.classList.toggle('is-hidden', !isOther);
                input.disabled = !isOther;
                input.required = isOther;

                if (isOther) {
                    window.setTimeout(() => input.focus(), 0);
                }
            };

            select.addEventListener('change', update);
            update();
        });
    }
};

document.addEventListener('DOMContentLoaded', () => window.dailyVitalsExerciseOther.initialize());
document.addEventListener('enhancedload', () => window.dailyVitalsExerciseOther.initialize());
