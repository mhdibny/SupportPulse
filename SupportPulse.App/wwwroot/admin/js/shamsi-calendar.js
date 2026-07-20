/**
 * Shamsi (Jalali) Calendar – True Persian Glassmorphism (Fixed Global)
 * Accurate Persian calendar with correct date conversion
 * Outputs: yyyy/MM/dd
 */
(function () {
    'use strict';

    // ========== Accurate Persian Calendar Engine ==========
    const persianMonthNames = [
        'فروردین', 'اردیبهشت', 'خرداد', 'تیر',
        'مرداد', 'شهریور', 'مهر', 'آبان',
        'آذر', 'دی', 'بهمن', 'اسفند'
    ];

    const persianWeekDays = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج'];

    function gregorianToJalali(gy, gm, gd) {
        let g_d_m = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334];
        let gy2 = (gm > 2) ? gy + 1 : gy;
        let days = 355666 + (365 * gy) + Math.floor((gy2 + 3) / 4) - Math.floor((gy2 + 99) / 100) + Math.floor((gy2 + 399) / 400) + gd + g_d_m[gm - 1];
        let jy = -1595 + (33 * Math.floor(days / 12053));
        days %= 12053;
        jy += 4 * Math.floor(days / 1461);
        days %= 1461;
        if (days > 365) {
            jy += Math.floor((days - 1) / 365);
            days = (days - 1) % 365;
        }
        let jm, jd;
        if (days < 186) {
            jm = 1 + Math.floor(days / 31);
            jd = 1 + (days % 31);
        } else {
            jm = 7 + Math.floor((days - 186) / 30);
            jd = 1 + ((days - 186) % 30);
        }
        return [jy, jm, jd];
    }

    function jalaliToGregorian(jy, jm, jd) {
        let gy, gm, gd;
        jy += 1595;
        let days = -355668 + (365 * jy) + (Math.floor(jy / 33) * 8) + Math.floor(((jy % 33) + 3) / 4);
        if (jm < 7) {
            days += (jm - 1) * 31;
        } else {
            days += ((jm - 7) * 30) + 186;
        }
        days += jd - 1;
        gy = 400 * Math.floor(days / 146097);
        days %= 146097;
        if (days > 36524) {
            days--;
            gy += 100 * Math.floor(days / 36524);
            days %= 36524;
            if (days >= 365) days++;
        }
        gy += 4 * Math.floor(days / 1461);
        days %= 1461;
        if (days > 365) {
            gy += Math.floor((days - 1) / 365);
            days = (days - 1) % 365;
        }
        gd = days + 1;
        let sal_a = [0, 31, ((gy % 4 == 0 && gy % 100 != 0) || (gy % 400 == 0)) ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
        gm = 1;
        while (gm < 13 && gd > sal_a[gm]) {
            gd -= sal_a[gm];
            gm++;
        }
        return [gy, gm, gd];
    }

    // خروجی سراسری برای استفاده در admin.js
    window.gregorianToJalali = gregorianToJalali;
    window.jalaliToGregorian = jalaliToGregorian;

    function isPersianLeapYear(year) {
        let g = jalaliToGregorian(year, 12, 30);
        let nextDay = new Date(g[0], g[1] - 1, g[2] + 1);
        return nextDay.getMonth() === 2;
    }

    function getDaysInPersianMonth(year, month) {
        if (month <= 6) return 31;
        if (month <= 11) return 30;
        return isPersianLeapYear(year) ? 30 : 29;
    }

    function getPersianToday() {
        const now = new Date();
        return gregorianToJalali(now.getFullYear(), now.getMonth() + 1, now.getDate());
    }

    // ========== Calendar UI ==========
    function createCalendar(inputElement) {
        const wrapper = document.createElement('span');
        wrapper.className = 'calendar-wrapper';
        inputElement.parentNode.insertBefore(wrapper, inputElement);
        wrapper.appendChild(inputElement);
        inputElement.setAttribute('autocomplete', 'off');
        inputElement.setAttribute('readonly', 'readonly');

        const popup = document.createElement('div');
        popup.className = 'calendar-popup';
        wrapper.appendChild(popup);

        let currentYear, currentMonth;
        let selectedDate = null;

        function render() {
            popup.innerHTML = '';

            const header = document.createElement('div');
            header.className = 'calendar-header';

            const prevBtn = document.createElement('button');
            prevBtn.className = 'calendar-nav-btn';
            prevBtn.innerHTML = '<i class="fas fa-chevron-right"></i>';
            prevBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                if (currentMonth === 1) { currentYear--; currentMonth = 12; }
                else currentMonth--;
                render();
            });

            const monthYear = document.createElement('span');
            monthYear.className = 'calendar-month-year';
            monthYear.textContent = persianMonthNames[currentMonth - 1] + ' ' + currentYear;

            const nextBtn = document.createElement('button');
            nextBtn.className = 'calendar-nav-btn';
            nextBtn.innerHTML = '<i class="fas fa-chevron-left"></i>';
            nextBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                if (currentMonth === 12) { currentYear++; currentMonth = 1; }
                else currentMonth++;
                render();
            });

            header.appendChild(prevBtn);
            header.appendChild(monthYear);
            header.appendChild(nextBtn);
            popup.appendChild(header);

            const grid = document.createElement('div');
            grid.className = 'calendar-grid';
            persianWeekDays.forEach((name, i) => {
                const dn = document.createElement('div');
                dn.className = 'calendar-day-name' + (i === 6 ? ' weekend' : '');
                dn.textContent = name;
                grid.appendChild(dn);
            });

            const [todayJY, todayJM, todayJD] = getPersianToday();
            const daysInMonth = getDaysInPersianMonth(currentYear, currentMonth);
            const firstDayGreg = jalaliToGregorian(currentYear, currentMonth, 1);
            const firstDayDate = new Date(firstDayGreg[0], firstDayGreg[1] - 1, firstDayGreg[2]);
            let persianWeekDay = (firstDayDate.getDay() + 1) % 7;

            for (let i = 0; i < persianWeekDay; i++) {
                const empty = document.createElement('div');
                empty.className = 'calendar-day other-month';
                grid.appendChild(empty);
            }

            for (let d = 1; d <= daysInMonth; d++) {
                const dayBtn = document.createElement('div');
                dayBtn.className = 'calendar-day';
                dayBtn.textContent = d;

                if (currentYear === todayJY && currentMonth === todayJM && d === todayJD) {
                    dayBtn.classList.add('today');
                }
                if (selectedDate && selectedDate[0] === currentYear && selectedDate[1] === currentMonth && selectedDate[2] === d) {
                    dayBtn.classList.add('selected');
                }

                const currentDayGreg = jalaliToGregorian(currentYear, currentMonth, d);
                const currentDayDate = new Date(currentDayGreg[0], currentDayGreg[1] - 1, currentDayGreg[2]);
                if (currentDayDate.getDay() === 6) {
                    dayBtn.classList.add('weekend');
                }

                dayBtn.addEventListener('click', (e) => {
                    e.stopPropagation();
                    selectedDate = [currentYear, currentMonth, d];
                    const formatted = currentYear + '/' + String(currentMonth).padStart(2, '0') + '/' + String(d).padStart(2, '0');
                    inputElement.value = formatted;
                    popup.classList.remove('open');
                });
                grid.appendChild(dayBtn);
            }

            popup.appendChild(grid);

            const footer = document.createElement('div');
            footer.className = 'calendar-footer';

            const todayBtn = document.createElement('button');
            todayBtn.className = 'calendar-today-btn';
            todayBtn.textContent = 'امروز';
            todayBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                currentYear = todayJY;
                currentMonth = todayJM;
                render();
            });

            const selectedDisplay = document.createElement('span');
            selectedDisplay.className = 'calendar-selected-date';
            if (selectedDate) {
                selectedDisplay.textContent = selectedDate[0] + '/' + String(selectedDate[1]).padStart(2, '0') + '/' + String(selectedDate[2]).padStart(2, '0');
            }

            footer.appendChild(todayBtn);
            footer.appendChild(selectedDisplay);
            popup.appendChild(footer);
        }

        const [initY, initM] = getPersianToday();
        currentYear = initY;
        currentMonth = initM;
        render();

        inputElement.addEventListener('click', (e) => {
            e.stopPropagation();
            const isOpen = popup.classList.contains('open');
            document.querySelectorAll('.calendar-popup.open').forEach(p => p.classList.remove('open'));
            if (!isOpen) {
                popup.classList.add('open');
                if (inputElement.value) {
                    const parts = inputElement.value.split('/');
                    if (parts.length === 3) {
                        const y = parseInt(parts[0]), m = parseInt(parts[1]), d = parseInt(parts[2]);
                        if (!isNaN(y) && !isNaN(m) && !isNaN(d)) {
                            currentYear = y;
                            currentMonth = m;
                            selectedDate = [y, m, d];
                        }
                    }
                }
                render();
            }
        });

        document.addEventListener('click', (e) => {
            if (!wrapper.contains(e.target)) {
                popup.classList.remove('open');
            }
        });
    }

    window.initShamsiDatePicker = function (input) {
        if (input) createCalendar(input);
    };
})();