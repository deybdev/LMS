// Admin Logs Management JavaScript
$(document).ready(function() {
    initializeLogsManagement();
});

function initializeLogsManagement() {
    setupFilters();
    setupSearch();
    setupDateRange();
    updateRelativeTimes();
    loadLogsData();
    
    // Update relative times every minute
    setInterval(updateRelativeTimes, 60000);
    
    console.log('Logs management initialized');
}

// Setup filter functionality
function setupFilters() {
    $('#logLevelFilter, #logCategoryFilter, #timeRangeFilter').on('change', function() {
        filterLogs();
    });
}

// Setup search functionality
function setupSearch() {
    let searchTimeout;
    $('#logSearchInput').on('input', function() {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            filterLogs();
        }, 300);
    });
}

// Setup date range functionality
function setupDateRange() {
    $('#timeRangeFilter').on('change', function() {
        const customDateRange = $('#customDateRange');
        if ($(this).val() === 'custom') {
            customDateRange.show();
        } else {
            customDateRange.hide();
        }
    });
    
    $('#startDate, #endDate').on('change', function() {
        filterLogs();
    });
}

// Filter logs based on criteria
function filterLogs() {
    const searchTerm = $('#logSearchInput').val().toLowerCase();
    const levelFilter = $('#logLevelFilter').val();
    const categoryFilter = $('#logCategoryFilter').val();
    const timeRangeFilter = $('#timeRangeFilter').val();
    
    let visibleRows = 0;
    
    $('.log-row').each(function() {
        const $row = $(this);
        const message = $row.find('.message-preview').text().toLowerCase();
        const user = $row.find('.user-name').text().toLowerCase();
        const ipAddress = $row.find('.ip-address').text().toLowerCase();
        const level = $row.find('.log-level').text().toLowerCase().trim();
        const category = $row.find('.category-tag').text().toLowerCase().trim();
        
        let showRow = true;
        
        // Search filter
        if (searchTerm && !message.includes(searchTerm) && 
            !user.includes(searchTerm) && !ipAddress.includes(searchTerm)) {
            showRow = false;
        }
        
        // Level filter
        if (levelFilter && !level.includes(levelFilter)) {
            showRow = false;
        }
        
        // Category filter
        if (categoryFilter && !category.includes(categoryFilter)) {
            showRow = false;
        }
        
        // Time range filter (simplified - in real implementation would filter by actual dates)
        if (timeRangeFilter === 'custom') {
            const startDate = $('#startDate').val();
            const endDate = $('#endDate').val();
            // Add date filtering logic here
        }
        
        if (showRow) {
            $row.show();
            visibleRows++;
        } else {
            $row.hide();
        }
    });
    
    updatePaginationInfo(visibleRows);
}

// Update pagination information
function updatePaginationInfo(visibleRows) {
    $('.pagination-info span').text(`Showing 1-${Math.min(visibleRows, 5)} of ${visibleRows} logs`);
}

// Update relative timestamps
function updateRelativeTimes() {
    $('.time-relative').each(function() {
        const $element = $(this);
        const timeText = $element.closest('.timestamp').find('.time-main').text();
        const logTime = new Date(timeText);
        const now = new Date();
        const diffMinutes = Math.floor((now - logTime) / (1000 * 60));
        
        let relativeText = '';
        if (diffMinutes < 1) {
            relativeText = 'Just now';
        } else if (diffMinutes < 60) {
            relativeText = `${diffMinutes} minutes ago`;
        } else if (diffMinutes < 1440) {
            const hours = Math.floor(diffMinutes / 60);
            relativeText = `${hours} hour${hours > 1 ? 's' : ''} ago`;
        } else {
            const days = Math.floor(diffMinutes / 1440);
            relativeText = `${days} day${days > 1 ? 's' : ''} ago`;
        }
        
        $element.text(relativeText);
    });
}

// View log details in modal
function viewLogDetails(logId) {
    // Get log data (in real implementation, this would fetch from server)
    const logData = getLogData(logId);
    
    // Populate modal with log data
    populateLogModal(logData);
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('logDetailsModal'));
    modal.show();
}

// Get log data by ID (simulated data)
function getLogData(logId) {
    const logsData = {
        1: {
            level: 'error',
            levelText: 'Error',
            timestamp: '2024-10-30 14:32:15',
            category: 'authentication',
            categoryText: 'Authentication',
            message: 'Failed login attempt for user \'admin@g2academy.edu\'. Invalid password provided. This is the 3rd failed attempt in the last 10 minutes. Account may be compromised.',
            user: 'admin@g2academy.edu',
            ipAddress: '192.168.1.105',
            sessionId: 'sess_123456789',
            userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
            requestUrl: '/Admin/Login',
            requestMethod: 'POST',
            responseCode: '401',
            stackTrace: 'at AuthenticationController.Login(LoginModel model) in C:\\LMS\\Controllers\\AuthenticationController.cs:line 45\nat Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.Execute()\nat Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Next()'
        },
        2: {
            level: 'warning',
            levelText: 'Warning',
            timestamp: '2024-10-30 14:28:42',
            category: 'system',
            categoryText: 'System',
            message: 'System memory usage has reached 85% capacity. Current usage: 6.8GB / 8GB. Consider restarting services or adding more memory.',
            user: 'System',
            ipAddress: 'localhost',
            sessionId: 'N/A',
            userAgent: 'System Monitor',
            requestUrl: '/api/system/health',
            requestMethod: 'GET',
            responseCode: '200',
            stackTrace: 'No stack trace available for system monitoring logs.'
        },
        3: {
            level: 'info',
            levelText: 'Info',
            timestamp: '2024-10-30 14:25:18',
            category: 'user',
            categoryText: 'User Actions',
            message: 'User \'sarah.johnson@g2academy.edu\' logged in successfully from Chrome browser. Session ID: sess_123456789. Location: Philippines.',
            user: 'Dr. Sarah Johnson',
            ipAddress: '192.168.1.102',
            sessionId: 'sess_987654321',
            userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
            requestUrl: '/Teacher/Dashboard',
            requestMethod: 'GET',
            responseCode: '200',
            stackTrace: 'No errors occurred.'
        },
        4: {
            level: 'info',
            levelText: 'Info',
            timestamp: '2024-10-30 14:22:33',
            category: 'course',
            categoryText: 'Course Management',
            message: 'New course \'Advanced Web Development\' (CS401) created by Dr. Sarah Johnson. Course assigned to Computer Studies department. Initial enrollment capacity: 50 students.',
            user: 'Dr. Sarah Johnson',
            ipAddress: '192.168.1.102',
            sessionId: 'sess_987654321',
            userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
            requestUrl: '/Teacher/Courses/Create',
            requestMethod: 'POST',
            responseCode: '201',
            stackTrace: 'Course created successfully.'
        },
        5: {
            level: 'error',
            levelText: 'Error',
            timestamp: '2024-10-30 14:18:55',
            category: 'database',
            categoryText: 'Database',
            message: 'Database connection timeout occurred while executing query: SELECT * FROM Users WHERE Active = 1. Connection took longer than 30 seconds. Database may be overloaded.',
            user: 'System',
            ipAddress: 'localhost',
            sessionId: 'N/A',
            userAgent: 'Database Engine',
            requestUrl: '/api/users',
            requestMethod: 'GET',
            responseCode: '500',
            stackTrace: 'System.Data.SqlClient.SqlException: Timeout expired. The timeout period elapsed prior to completion of the operation or the server is not responding.\nat System.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)'
        }
    };
    
    return logsData[logId] || logsData[1];
}

// Populate log modal with data
function populateLogModal(logData) {
    $('#modalLogLevel').removeClass().addClass(`log-level ${logData.level}`).html(`<i class="fas fa-${getLogLevelIcon(logData.level)}"></i> ${logData.levelText}`);
    $('#modalTimestamp').text(logData.timestamp);
    $('#modalCategory').removeClass().addClass(`category-tag ${logData.category}`).text(logData.categoryText);
    $('#modalUser').text(logData.user);
    $('#modalIpAddress').text(logData.ipAddress);
    $('#modalSessionId').text(logData.sessionId);
    $('#modalMessage').text(logData.message);
    $('#modalUserAgent').text(logData.userAgent);
    $('#modalRequestUrl').text(logData.requestUrl);
    $('#modalRequestMethod').text(logData.requestMethod);
    $('#modalResponseCode').text(logData.responseCode);
    $('#modalStackTrace').html(logData.stackTrace.replace(/\n/g, '<br>'));
}

// Get log level icon
function getLogLevelIcon(level) {
    const icons = {
        'error': 'times-circle',
        'warning': 'exclamation-triangle',
        'info': 'info-circle',
        'debug': 'bug'
    };
    return icons[level] || 'info-circle';
}

// Delete log
function deleteLog(logId) {
    if (confirm('Are you sure you want to delete this log entry? This action cannot be undone.')) {
        const $row = $(`.log-row[data-log-id="${logId}"]`);
        
        $row.fadeOut(300, function() {
            $(this).remove();
            updatePaginationInfo($('.log-row:visible').length);
            showAlert('success', 'Log entry deleted successfully.');
        });
    }
}

// Delete log from modal
function deleteLogFromModal() {
    // Get log ID from modal context (you'd need to store this when opening modal)
    const logId = 1; // This should be stored when modal opens
    
    $('#logDetailsModal').modal('hide');
    deleteLog(logId);
}

// Export logs
function exportLogs() {
    showAlert('info', 'Preparing logs export... This may take a few moments.');
    
    setTimeout(() => {
        // Simulate export process
        const csvContent = generateLogsCSV();
        downloadCSV(csvContent, 'system_logs.csv');
        showAlert('success', 'Logs exported successfully! Check your downloads folder.');
    }, 2000);
}

// Generate CSV content for logs
function generateLogsCSV() {
    let csv = 'Timestamp,Level,Category,Message,User,IP Address\n';
    
    $('.log-row:visible').each(function() {
        const $row = $(this);
        const timestamp = $row.find('.time-main').text();
        const level = $row.find('.log-level').text().trim();
        const category = $row.find('.category-tag').text().trim();
        const message = $row.find('.message-preview').text().replace(/"/g, '""');
        const user = $row.find('.user-name').text();
        const ipAddress = $row.find('.ip-address').text();
        
        csv += `"${timestamp}","${level}","${category}","${message}","${user}","${ipAddress}"\n`;
    });
    
    return csv;
}

// Download CSV file
function downloadCSV(content, filename) {
    const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    
    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';
    
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// Refresh logs
function refreshLogs() {
    const $refreshBtn = $('.btn-refresh');
    const originalText = $refreshBtn.html();
    
    $refreshBtn.html('<i class="fas fa-spinner fa-spin"></i> Refreshing...');
    $refreshBtn.prop('disabled', true);
    
    setTimeout(() => {
        $refreshBtn.html(originalText);
        $refreshBtn.prop('disabled', false);
        updateRelativeTimes();
        showAlert('success', 'Logs refreshed successfully!');
    }, 1500);
}

// Clear all logs
function clearLogs() {
    if (confirm('Are you sure you want to clear all logs? This action cannot be undone and will permanently delete all log entries.')) {
        if (confirm('This will permanently delete ALL log entries. Are you absolutely sure?')) {
            $('.log-row').fadeOut(300, function() {
                $(this).remove();
                updatePaginationInfo(0);
                
                // Show empty state
                $('#logsTableBody').html(`
                    <tr>
                        <td colspan="7" class="text-center" style="padding: 3rem;">
                            <div style="color: #6c757d;">
                                <i class="fas fa-inbox" style="font-size: 3rem; margin-bottom: 1rem;"></i>
                                <h5>No logs found</h5>
                                <p>All logs have been cleared. New logs will appear here as they are generated.</p>
                            </div>
                        </td>
                    </tr>
                `);
                
                showAlert('success', 'All logs have been cleared successfully.');
            });
        }
    }
}

// Load logs data (simulated)
function loadLogsData() {
    updatePaginationInfo($('.log-row').length);
}

// Alert system
function showAlert(type, message) {
    const alertId = 'alert-' + Date.now();
    const alertHtml = `
        <div id="${alertId}" class="alert alert-${type} alert-dismissible fade show" role="alert" style="position: fixed; top: 20px; right: 20px; z-index: 9999; max-width: 300px; box-shadow: 0 4px 20px rgba(0,0,0,0.15);">
            <i class="fas fa-${type === 'success' ? 'check-circle' :
                type === 'danger' ? 'exclamation-circle' :
                    type === 'warning' ? 'exclamation-triangle' : 'info-circle'
            } me-2"></i>
            <span>${message}</span>
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;

    $('body').append(alertHtml);

    setTimeout(function () {
        $(`#${alertId}`).fadeOut(300, function () {
            $(this).remove();
        });
    }, 5000);
}

// Pagination functionality
$('.pagination-number').on('click', function() {
    $('.pagination-number').removeClass('active');
    $(this).addClass('active');
    
    const pageNumber = $(this).text();
    console.log(`Loading page ${pageNumber}`);
    // In real implementation, this would load new log data
});

$('.pagination-btn').on('click', function() {
    if ($(this).prop('disabled')) return;
    
    const isNext = $(this).text().includes('Next');
    const currentPage = parseInt($('.pagination-number.active').text());
    const newPage = isNext ? currentPage + 1 : currentPage - 1;
    
    console.log(`Navigating to page ${newPage}`);
    
    // Update active page (simplified)
    $('.pagination-number').removeClass('active');
    $(`.pagination-number:contains("${newPage}")`).addClass('active');
});

console.log('Admin Logs Management JavaScript loaded successfully');