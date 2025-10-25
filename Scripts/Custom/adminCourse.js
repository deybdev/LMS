// Admin Course Management JavaScript - Modal Layout
$(document).ready(function() {
    initializeCourseManagement();
});

function initializeCourseManagement() {
    setupFilters();
    setupSearch();
    setupCardInteractions();
    loadCourseData();
    
    console.log('Course management initialized with modal layout');
}

// Setup filter functionality
function setupFilters() {
    $('#statusFilter, #departmentFilter, #sortBy').on('change', function() {
        filterCourses();
    });
}

// Setup search functionality
function setupSearch() {
    let searchTimeout;
    $('#courseSearchInput').on('input', function() {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            filterCourses();
        }, 300);
    });
}

// Setup card interactions
function setupCardInteractions() {
    // Add hover effects
    $('.course-card').on('mouseenter', function() {
        $(this).addClass('hovered');
    }).on('mouseleave', function() {
        $(this).removeClass('hovered');
    });
}

// Open course details modal
function openCourseModal(courseId) {
    // Get course data
    const courseData = getCourseData(courseId);
    
    // Populate modal with course data
    populateModal(courseData);
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('courseDetailsModal'));
    modal.show();
    
    // Load initial tab content
    loadStudentsData(courseId);
}

// Get course data by ID (simulated data)
function getCourseData(courseId) {
    const coursesData = {
        1: {
            title: 'Advanced Web Development',
            code: 'CS401',
            department: 'Computer Studies',
            status: 'Active',
            statusClass: 'status-active',
            description: 'Learn modern web technologies including React, Node.js, and database integration. This comprehensive course covers frontend and backend development with hands-on projects.',
            teacher: 'Dr. Sarah Johnson',
            teacherEmail: 'sarah.johnson@g2academy.edu',
            studentCount: 45,
            materialCount: 24,
            engagementRate: '87%',
            createdDate: 'Sep 15, 2024'
        },
        2: {
            title: 'Database Management Systems',
            code: 'CS302',
            department: 'Information Technology',
            status: 'Featured',
            statusClass: 'status-featured',
            description: 'Comprehensive course on SQL, NoSQL databases, and data modeling principles. Learn database design, optimization, and administration.',
            teacher: 'Prof. Michael Chen',
            teacherEmail: 'michael.chen@g2academy.edu',
            studentCount: 38,
            materialCount: 18,
            engagementRate: '92%',
            createdDate: 'Aug 28, 2024'
        },
        3: {
            title: 'Mobile App Development',
            code: 'CS450',
            department: 'Computer Studies',
            status: 'Active',
            statusClass: 'status-active',
            description: 'Build native and cross-platform mobile applications using React Native and Flutter. Create professional mobile apps for iOS and Android.',
            teacher: 'Dr. Emily Rodriguez',
            teacherEmail: 'emily.rodriguez@g2academy.edu',
            studentCount: 52,
            materialCount: 31,
            engagementRate: '83%',
            createdDate: 'Sep 22, 2024'
        },
        4: {
            title: 'Artificial Intelligence',
            code: 'CS501',
            department: 'Computer Studies',
            status: 'Active',
            statusClass: 'status-active',
            description: 'Introduction to AI concepts, machine learning algorithms, and neural networks. Explore the fundamentals of artificial intelligence.',
            teacher: 'Dr. Robert Kim',
            teacherEmail: 'robert.kim@g2academy.edu',
            studentCount: 29,
            materialCount: 15,
            engagementRate: '89%',
            createdDate: 'Oct 1, 2024'
        }
    };
    
    return coursesData[courseId] || coursesData[1];
}

// Populate modal with course data
function populateModal(courseData) {
    // Update modal title and basic info
    $('#modalCourseTitle').text(courseData.title);
    $('#modalCourseCode').text(courseData.code);
    $('#modalDepartment').text(courseData.department);
    $('#modalStatus').html(`<i class="fas fa-${courseData.status === 'Featured' ? 'star' : 'circle'}"></i> ${courseData.status}`);
    $('#modalStatus').attr('class', `status-badge ${courseData.statusClass}`);
    $('#modalCourseDescription').text(courseData.description);
    
    // Update teacher info
    $('#modalTeacherName').text(courseData.teacher);
    $('#modalTeacherEmail').text(courseData.teacherEmail);
    
    // Update stats
    $('#modalStudentCount').text(courseData.studentCount);
    $('#modalMaterialCount').text(courseData.materialCount);
    $('#modalEngagementRate').text(courseData.engagementRate);
    
    // Reset to first tab
    showModalTab('students');
}

// Show modal tab
function showModalTab(tabName) {
    // Hide all tabs
    $('.tab-pane').removeClass('active');
    $('.tab-btn').removeClass('active');
    
    // Show selected tab
    $(`#modal-${tabName}-tab`).addClass('active');
    
    // Activate correct tab button
    $('.tab-btn').each(function() {
        const onclickAttr = $(this).attr('onclick');
        if (onclickAttr && onclickAttr.includes(`'${tabName}'`)) {
            $(this).addClass('active');
        }
    });
    
    // Load content based on tab
    const courseId = 1; // You would get this from the current modal context
    
    switch(tabName) {
        case 'students':
            loadStudentsData(courseId);
            break;
        case 'materials':
            loadMaterialsData(courseId);
            break;
        case 'analytics':
            loadAnalyticsData(courseId);
            break;
    }
}

// Load students data for modal
function loadStudentsData(courseId) {
    const sampleStudents = [
        {
            name: 'John Smith',
            id: '2021001',
            email: 'john.smith@student.g2academy.edu',
            progress: 85,
            status: 'Active'
        },
        {
            name: 'Sarah Johnson',
            id: '2021002',
            email: 'sarah.johnson@student.g2academy.edu',
            progress: 92,
            status: 'Active'
        },
        {
            name: 'Michael Brown',
            id: '2021003',
            email: 'michael.brown@student.g2academy.edu',
            progress: 78,
            status: 'Active'
        },
        {
            name: 'Emily Davis',
            id: '2021004',
            email: 'emily.davis@student.g2academy.edu',
            progress: 91,
            status: 'Active'
        },
        {
            name: 'David Wilson',
            id: '2021005',
            email: 'david.wilson@student.g2academy.edu',
            progress: 87,
            status: 'Active'
        }
    ];
    
    let studentsHtml = '';
    sampleStudents.forEach(student => {
        studentsHtml += `
            <div class="student-item">
                <div class="student-avatar">
                    <i class="fas fa-user-circle"></i>
                </div>
                <div class="student-info">
                    <div class="student-name">${student.name}</div>
                    <div class="student-id">ID: ${student.id}</div>
                    <div class="student-email">${student.email}</div>
                </div>
                <div class="student-progress">
                    <div class="progress-bar">
                        <div class="progress-fill" style="width: ${student.progress}%"></div>
                    </div>
                    <span class="progress-text">${student.progress}%</span>
                </div>
                <div class="student-status">
                    <span class="status-badge status-active">${student.status}</span>
                </div>
            </div>
        `;
    });
    
    $('#modalStudentsList').html(studentsHtml);
}

// Load materials data for modal
function loadMaterialsData(courseId) {
    const sampleMaterials = [
        {
            name: 'Introduction to React.js',
            type: 'PDF',
            icon: 'fas fa-file-pdf',
            meta: 'PDF • 2.3 MB • Uploaded Sep 20, 2024',
            downloads: 'Downloaded 45 times'
        },
        {
            name: 'Building Your First React Component',
            type: 'Video',
            icon: 'fas fa-video',
            meta: 'Video • 45:30 • Uploaded Sep 22, 2024',
            downloads: 'Viewed 38 times'
        },
        {
            name: 'Assignment 1: Basic React App',
            type: 'Assignment',
            icon: 'fas fa-tasks',
            meta: 'Assignment • Due Oct 5, 2024',
            downloads: '32 submissions received'
        },
        {
            name: 'React Hooks Deep Dive',
            type: 'PDF',
            icon: 'fas fa-file-pdf',
            meta: 'PDF • 1.8 MB • Uploaded Sep 25, 2024',
            downloads: 'Downloaded 29 times'
        },
        {
            name: 'State Management Tutorial',
            type: 'Video',
            icon: 'fas fa-video',
            meta: 'Video • 1:12:30 • Uploaded Sep 28, 2024',
            downloads: 'Viewed 31 times'
        }
    ];
    
    let materialsHtml = '';
    sampleMaterials.forEach(material => {
        materialsHtml += `
            <div class="material-item">
                <div class="material-icon">
                    <i class="${material.icon}"></i>
                </div>
                <div class="material-info">
                    <div class="material-name">${material.name}</div>
                    <div class="material-meta">${material.meta}</div>
                    <div class="material-downloads">${material.downloads}</div>
                </div>
                <div class="material-actions">
                    <button class="btn btn-sm btn-outline-primary" onclick="previewMaterial('${material.name}')">
                        <i class="fas fa-eye"></i> ${material.type === 'Video' ? 'Watch' : 'View'}
                    </button>
                    <button class="btn btn-sm btn-outline-secondary" onclick="downloadMaterial('${material.name}')">
                        <i class="fas fa-${material.type === 'Video' ? 'chart-bar' : 'download'}"></i> 
                        ${material.type === 'Video' ? 'Stats' : 'Download'}
                    </button>
                </div>
            </div>
        `;
    });
    
    $('#modalMaterialsList').html(materialsHtml);
}

// Load analytics data for modal
function loadAnalyticsData(courseId) {
    const analyticsHtml = `
        <div class="analytics-grid">
            <div class="analytics-card">
                <h5>Engagement Rate</h5>
                <div class="metric-value">87%</div>
                <div class="metric-trend positive">+5% from last month</div>
            </div>
            <div class="analytics-card">
                <h5>Average Grade</h5>
                <div class="metric-value">85.2</div>
                <div class="metric-trend positive">+2.1 from last semester</div>
            </div>
            <div class="analytics-card">
                <h5>Material Downloads</h5>
                <div class="metric-value">1,247</div>
                <div class="metric-trend positive">+156 this week</div>
            </div>
            <div class="analytics-card">
                <h5>Assignment Submissions</h5>
                <div class="metric-value">98%</div>
                <div class="metric-trend positive">+3% from last week</div>
            </div>
            <div class="analytics-card">
                <h5>Active Students</h5>
                <div class="metric-value">42/45</div>
                <div class="metric-trend positive">93% participation</div>
            </div>
            <div class="analytics-card">
                <h5>Average Session Time</h5>
                <div class="metric-value">24min</div>
                <div class="metric-trend positive">+5min from last week</div>
            </div>
        </div>
    `;
    
    $('#modalAnalyticsContent').html(analyticsHtml);
}

// Filter courses based on search and filters
function filterCourses() {
    const searchTerm = $('#courseSearchInput').val().toLowerCase();
    const statusFilter = $('#statusFilter').val();
    const departmentFilter = $('#departmentFilter').val();
    const sortBy = $('#sortBy').val();
    
    let visibleCards = 0;
    
    $('.course-card').each(function() {
        const $card = $(this);
        const courseTitle = $card.find('.course-title').text().toLowerCase();
        const courseDescription = $card.find('.course-description').text().toLowerCase();
        const teacherName = $card.find('.teacher-name').text().toLowerCase();
        const department = $card.find('.department-tag').text();
        const status = $card.find('.status-badge').hasClass('status-active') ? 'active' : 
                      $card.find('.status-badge').hasClass('status-featured') ? 'featured' :
                      $card.find('.status-badge').hasClass('status-inactive') ? 'inactive' : 'archived';
        
        let showCard = true;
        
        // Search filter
        if (searchTerm && !courseTitle.includes(searchTerm) && 
            !courseDescription.includes(searchTerm) && 
            !teacherName.includes(searchTerm)) {
            showCard = false;
        }
        
        // Status filter
        if (statusFilter && status !== statusFilter) {
            showCard = false;
        }
        
        // Department filter
        if (departmentFilter && !department.includes(departmentFilter)) {
            showCard = false;
        }
        
        if (showCard) {
            $card.show();
            visibleCards++;
        } else {
            $card.hide();
        }
    });
    
    updatePaginationInfo(visibleCards);
}

// Update pagination information
function updatePaginationInfo(visibleCards) {
    $('.pagination-info span').text(`Showing 1-${Math.min(visibleCards, 4)} of ${visibleCards} courses`);
}

// Load course data (simulated)
function loadCourseData() {
    updatePaginationInfo($('.course-card').length);
}

// Export courses functionality
function exportCourses() {
    showAlert('info', 'Preparing course export... This may take a few moments.');
    
    setTimeout(() => {
        showAlert('success', 'Course data exported successfully! Check your downloads folder.');
    }, 2000);
}

// Refresh courses functionality
function refreshCourses() {
    const $refreshBtn = $('.btn-refresh');
    const originalText = $refreshBtn.html();
    
    $refreshBtn.html('<i class="fas fa-spinner fa-spin"></i> Refreshing...');
    $refreshBtn.prop('disabled', true);
    
    setTimeout(() => {
        $refreshBtn.html(originalText);
        $refreshBtn.prop('disabled', false);
        showAlert('success', 'Course data refreshed successfully!');
        
        loadCourseData();
    }, 1500);
}

// Toggle course featured status
function toggleFeature(courseId) {
    const $btn = $(`.course-card[data-course-id="${courseId}"] .feature-btn`);
    const $statusBadge = $btn.closest('.course-card').find('.status-badge');
    
    if ($btn.hasClass('featured')) {
        // Remove from featured
        $btn.removeClass('featured');
        $statusBadge.removeClass('status-featured').addClass('status-active');
        $statusBadge.html('<i class="fas fa-circle"></i> Active');
        showAlert('success', 'Course removed from featured courses.');
    } else {
        // Add to featured
        $btn.addClass('featured');
        $statusBadge.removeClass('status-active').addClass('status-featured');
        $statusBadge.html('<i class="fas fa-star"></i> Featured');
        showAlert('success', 'Course added to featured courses.');
    }
}

// Toggle course status
function toggleStatus(courseId) {
    const $card = $(`.course-card[data-course-id="${courseId}"]`);
    const $statusBadge = $card.find('.status-badge');
    const $statusBtn = $card.find('.status-btn');
    
    if ($statusBadge.hasClass('status-active') || $statusBadge.hasClass('status-featured')) {
        // Deactivate course
        $statusBadge.removeClass('status-active status-featured').addClass('status-inactive');
        $statusBadge.html('<i class="fas fa-circle"></i> Inactive');
        $statusBtn.find('i').removeClass('fa-toggle-on').addClass('fa-toggle-off');
        showAlert('warning', 'Course has been deactivated.');
    } else {
        // Activate course
        $statusBadge.removeClass('status-inactive').addClass('status-active');
        $statusBadge.html('<i class="fas fa-circle"></i> Active');
        $statusBtn.find('i').removeClass('fa-toggle-off').addClass('fa-toggle-on');
        showAlert('success', 'Course has been activated.');
    }
}

// Show more options for course
function showMoreOptions(courseId) {
    const options = [
        'Generate Course Report',
        'Archive Course',
        'Delete Course',
        'View Full Analytics',
        'Export Student List'
    ];
    
    console.log(`More options for course ${courseId}:`, options);
    showAlert('info', 'More options menu would appear here.');
}

// Material interaction functions
function previewMaterial(materialName) {
    showAlert('info', `Opening preview for: ${materialName}`);
}

function downloadMaterial(materialName) {
    showAlert('success', `Downloading: ${materialName}`);
}

// Generate course report
function generateCourseReport() {
    showAlert('info', 'Generating course report... This may take a few moments.');
    
    setTimeout(() => {
        showAlert('success', 'Course report generated successfully! Check your downloads folder.');
    }, 3000);
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

console.log('Admin Course Management JavaScript (Modal Layout) loaded successfully');