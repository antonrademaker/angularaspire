import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function dateRangeValidator(startDateControlName: string, endDateControlName: string): ValidatorFn {
  return (formGroup: AbstractControl): ValidationErrors | null => {
    const startDateControl = formGroup.get(startDateControlName);
    const endDateControl = formGroup.get(endDateControlName);

    if (!startDateControl || !endDateControl) {
      return null;
    }

    const startDate = new Date(startDateControl.value);
    const endDate = new Date(endDateControl.value);

    if (startDateControl.value && endDateControl.value && startDate > endDate) {
      endDateControl.setErrors({ dateRange: true });
      return { dateRange: true };
    } else {
      // Only clear the error if it was set by this validator
      if (endDateControl.hasError('dateRange')) {
        endDateControl.setErrors(null);
      }
      return null;
    }
  };
}
