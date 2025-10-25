// Admin Dashboard JavaScript
$(document).ready(function () {
    initializeDashboard();
});

function initializeDashboard() {
    updateDateTime();
    animateCounters();
    initializeChart();
    refreshActivities();
    startRealTimeUpdates();

    // Update date/time every second
    setInterval(updateDateTime, 1000);

    // Refresh activities every 30 seconds
    setInterval(refreshActivities, 30000);
}

// Update current date and time
function updateDateTime() {
    const now = new Date();
    const dateOptions = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
    const timeOptions = { hour: '2-digit', minute: '2-digit', second: '2-digit' };

    $('#currentDate').text(now.toLocaleDateString('en-US', dateOptions));
    $('#currentTime').text(now.toLocaleTimeString('en-US', timeOptions));
}

// Animate counter numbers
function animateCounters() {
    $('.stat-number').each(function () {
        const $this = $(this);
        const target = $this.data('target');
        if (!target) return;

        const targetStr = target.toString();
        const numericTarget = parseInt(targetStr.replace(/[,%]/g, ''));

        $({ counter: 0 }).animate(
            { counter: numericTarget },
            {
                duration: 2000,
                easing: 'swing',
                step: function () {
                    if (targetStr.includes(',')) {
                        $this.text(Math.ceil(this.counter).toLocaleString());
                    } else if (targetStr.includes('%')) {
                        $this.text(Math.ceil(this.counter) + '%');
                    } else {
                        $this.text(Math.ceil(this.counter));
                    }
                },
                complete: function () {
                    $this.text(target);
                },
            }
        );
    });
}

// Initialize engagement chart
function initializeChart() {
    const ctx = document.getElementById('engagementChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
            datasets: [
                {
                    label: 'Student Engagement',
                    data: [85, 92, 78, 88, 96, 82, 90],
                    borderColor: '#1852AC',
                    backgroundColor: 'rgba(24, 82, 172, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointBackgroundColor: '#1852AC',
                    pointBorderColor: '#ffffff',
                    pointBorderWidth: 2,
                    pointRadius: 6,
                    pointHoverRadius: 8,
                },
            ],
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100,
                    ticks: { callback: (value) => value + '%' },
                },
                x: { grid: { display: false } },
            },
        },
    });
}

// Refresh activities (simulate updates)
function refreshActivities() {
    const activities = [
        { iconClass: 'fas fa-user-plus', text: 'New student <strong>Alex Johnson</strong> registered', time: 'Just now' },
        { iconClass: 'fas fa-file-upload', text: '<strong>Maria Garcia</strong> submitted assignment', time: '3 minutes ago' },
        { iconClass: 'fas fa-plus-circle', text: 'New course <strong>"Machine Learning Basics"</strong> created', time: '8 minutes ago' },
        { iconClass: 'fas fa-user-plus', text: 'New teacher <strong>Prof. Robert Chen</strong> joined', time: '12 minutes ago' },
    ];

    const html = activities
        .map(
            (a) => `
        <div class="activity-item">
            <div class="activity-icon"><i class="${a.iconClass}"></i></div>
            <div class="activity-details">
                <p class="activity-text">${a.text}</p>
                <span class="activity-time">${a.time}</span>
            </div>
        </div>`
        )
        .join('');

    $('.activity-list').html(html);
}

// Start real-time updates
function startRealTimeUpdates() {
    setInterval(updateStats, 10000);
    setInterval(updateSystemStatus, 15000);
}

// Update statistics dynamically
function updateStats() {
    const stats = [
        { selector: '.students .stat-number', base: 1247, variation: 5 },
        { selector: '.teachers .stat-number', base: 89, variation: 2 },
        { selector: '.courses .stat-number', base: 156, variation: 3 },
        { selector: '.engagement .stat-number', base: 92, variation: 5 },
    ];

    stats.forEach((stat) => {
        const variation = Math.floor(Math.random() * stat.variation) - Math.floor(stat.variation / 2);
        let newValue = stat.base + variation;

        if (stat.selector.includes('engagement')) {
            newValue = Math.min(Math.max(newValue, 85), 99);
            $(stat.selector).text(newValue + '%');
        } else {
            $(stat.selector).text(newValue.toLocaleString());
        }
    });
}

// Update system status
function updateSystemStatus() {
    const statusItems = [
        { label: 'Server Uptime', min: 99.5, max: 100 },
        { label: 'Storage Used', min: 65, max: 70 },
        { label: 'Active Sessions', min: 320, max: 380 },
    ];

    statusItems.forEach((item) => {
        const randomValue = Math.random() * (item.max - item.min) + item.min;
        const $statusItem = $(`.status-item:contains("${item.label}")`);

        if (!$statusItem.length) return;

        const valueText =
            item.label === 'Active Sessions'
                ? Math.round(randomValue)
                : randomValue.toFixed(1).replace(/\.0$/, '') + '%';

        $statusItem.find('.status-value').text(valueText);

        const width =
            item.label === 'Active Sessions'
                ? (randomValue / 400) * 100
                : randomValue;

        $statusItem.find('.status-progress').css('width', width + '%');
    });
}
