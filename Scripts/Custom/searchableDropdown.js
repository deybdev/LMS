class SearchableDropdown {
    constructor(options) {
        Object.assign(this, {
            searchInputId: options.searchInputId,
            searchUrl: options.searchUrl,
            onSelect: options.onSelect,
            resultsDropdownId: options.resultsDropdownId || `${options.searchInputId}Dropdown`,
            resultsListId: options.resultsListId || `${options.searchInputId}List`,
            noResultsMessageId: options.noResultsMessageId || `${options.searchInputId}NoResults`,
            loadingId: options.loadingId || `${options.searchInputId}Loading`,
            resultsCountId: options.resultsCountId || `${options.searchInputId}Count`,
            placeholder: options.placeholder || 'Type to search...',
            minChars: options.minChars || 1,
            debounceDelay: options.debounceDelay || 300,
            maxResults: options.maxResults || 20,
            renderResultItem: options.renderResultItem || this.defaultRenderResultItem.bind(this),
            formatResultData: options.formatResultData || this.defaultFormatResultData.bind(this),
            getExcludedIds: options.getExcludedIds || (() => []),
            additionalParams: options.additionalParams || {},
            searchTimeout: null
        });
        
        this.init();
    }

    init() {
        this.$searchInput = $(`#${this.searchInputId}`);
        this.$dropdown = $(`#${this.resultsDropdownId}`);
        this.$resultsList = $(`#${this.resultsListId}`);
        this.$noResults = $(`#${this.noResultsMessageId}`);
        this.$loading = $(`#${this.loadingId}`);
        this.$resultsCount = $(`#${this.resultsCountId}`);

        this.$searchInput.attr('placeholder', this.placeholder);
        this.attachEvents();
    }

    attachEvents() {
        this.$searchInput.on('input', (e) => this.handleSearch(e.target.value));
        
        $(document).on('click', (e) => {
            if (!$(e.target).closest(`#${this.searchInputId}, #${this.resultsDropdownId}`).length) {
                this.hideDropdown();
            }
        });
    }

    handleSearch(term = '') {
        clearTimeout(this.searchTimeout);
        term = term.trim();

        if (term.length < this.minChars) {
            this.hideDropdown();
            return;
        }

        this.showLoading();
        this.searchTimeout = setTimeout(() => this.performSearch(term), this.debounceDelay);
    }

    performSearch(term) {
        $.ajax({
            url: this.searchUrl,
            type: 'GET',
            data: { term, excludeIds: this.getExcludedIds(), ...this.additionalParams },
            traditional: true,
            success: (data) => this.handleSearchSuccess(data),
            error: (error) => this.handleSearchError(error)
        });
    }

    handleSearchSuccess(data) {
        this.$loading.hide();
        const results = data.results || data || [];

        if (results.length > 0) {
            this.renderResults(results);
            this.updateResultsCount(results.length);
            this.$noResults.hide();
        } else {
            this.showNoResults();
        }
    }

    handleSearchError(error) {
        console.error('Search error:', error);
        this.$loading.hide();
        this.$resultsList.empty();
        this.$noResults.show();
        this.updateResultsCount(0, 'Error searching');
    }

    renderResults(results) {
        this.$resultsList.empty();
        results.forEach(result => {
            const formattedData = this.formatResultData(result);
            const $item = this.renderResultItem(formattedData);
            $item.on('click', () => this.selectItem(formattedData));
            this.$resultsList.append($item);
        });
        this.$dropdown.show();
    }

    selectItem(item) {
        this.onSelect?.(item);
        this.clearSearch();
    }

    clearSearch() {
        this.$searchInput.val('');
        this.hideDropdown();
    }

    showLoading() {
        this.$loading.show();
        this.$resultsList.empty();
        this.$noResults.hide();
        this.$dropdown.show();
    }

    showNoResults() {
        this.$resultsList.empty();
        this.$noResults.show();
        this.updateResultsCount(0);
    }

    hideDropdown() {
        this.$dropdown.hide();
    }

    updateResultsCount(count, customText) {
        const text = customText || (count === 0 ? 'No results found' : `${count} result${count !== 1 ? 's' : ''} found`);
        this.$resultsCount.text(text);
    }

    updateAdditionalParams(params) {
        Object.assign(this.additionalParams, params);
    }

    defaultFormatResultData(result) {
        return result;
    }

    defaultRenderResultItem(data) {
        return $(`
            <div class="search-result-item">
                <div class="result-header">
                    <span class="result-title">${data.title || data.name || 'N/A'}</span>
                </div>
            </div>
        `);
    }
}

/**
 * Course Search Dropdown
 */
class CourseSearchDropdown extends SearchableDropdown {
    constructor(options) {
        super({
            searchUrl: '/Course/SearchCourses',
            placeholder: 'Type course code or title to search...',
            ...options
        });
    }

    defaultFormatResultData(result) {
        return {
            id: result.Id,
            code: result.CourseCode,
            title: result.CourseTitle,
            description: result.Description
        };
    }

    defaultRenderResultItem(course) {
        return $(`
            <div class="search-result-item" data-id="${course.id}">
                <div class="result-header">
                    <span class="result-code">${course.code}</span>
                    <span class="result-title">${course.title}</span>
                </div>
                ${course.description ? `<div class="result-description">${course.description}</div>` : ''}
            </div>
        `);
    }
}

/**
 * Teacher Search Dropdown
 */
class TeacherSearchDropdown extends SearchableDropdown {
    constructor(options) {
        super({
            searchUrl: '/User/SearchTeachers',
            placeholder: 'Type teacher name or email to search...',
            ...options
        });
    }

    defaultFormatResultData(result) {
        return {
            id: result.Id,
            userId: result.UserID,
            firstName: result.FirstName,
            lastName: result.LastName,
            fullName: `${result.FirstName} ${result.LastName}`,
            email: result.Email,
            department: result.Department
        };
    }

    defaultRenderResultItem(teacher) {
        const initials = `${teacher.firstName[0]}${teacher.lastName[0]}`.toUpperCase();
        return $(`
            <div class="search-result-item" data-id="${teacher.id}">
                <div class="result-with-avatar">
                    <div class="result-avatar">${initials}</div>
                    <div class="result-info">
                        <div class="result-header">
                            <span class="result-name">${teacher.fullName}</span>
                            ${teacher.userId ? `<span class="result-id">${teacher.userId}</span>` : ''}
                        </div>
                        <div class="result-meta">
                            <span class="result-email">${teacher.email}</span>
                            ${teacher.department ? `<span class="result-department">${teacher.department}</span>` : ''}
                        </div>
                    </div>
                </div>
            </div>
        `);
    }
}

/**
 * Student Search Dropdown
 */
class StudentSearchDropdown extends SearchableDropdown {
    constructor(options) {
        super({
            searchUrl: '/User/SearchStudents',
            placeholder: 'Type student name or ID to search...',
            ...options
        });
    }

    defaultFormatResultData(result) {
        return {
            id: result.Id,
            studentId: result.UserID,
            firstName: result.FirstName,
            lastName: result.LastName,
            fullName: `${result.FirstName} ${result.LastName}`,
            email: result.Email,
            department: result.Department,
            program: result.Program
        };
    }

    defaultRenderResultItem(student) {
        const initials = `${student.firstName[0]}${student.lastName[0]}`.toUpperCase();
        return $(`
            <div class="search-result-item" data-id="${student.id}">
                <div class="result-with-avatar">
                    <div class="result-avatar">${initials}</div>
                    <div class="result-info">
                        <div class="result-header">
                            <span class="result-name">${student.fullName}</span>
                            ${student.studentId ? `<span class="result-id">${student.studentId}</span>` : ''}
                        </div>
                        <div class="result-meta">
                            <span class="result-email">${student.email}</span>
                            ${student.program ? `<span class="result-program">${student.program}</span>` : ''}
                        </div>
                    </div>
                </div>
            </div>
        `);
    }
}

/**
 * Course Assignment Form Manager
 */
class CourseAssignmentForm {
    constructor(options = {}) {
        Object.assign(this, {
            formId: options.formId || 'assignCourseForm' || 'editCurriculumForm',
            programSelectId: options.programSelectId || 'programSelect',
            yearLevelSelectId: options.yearLevelSelectId || 'yearLevelSelect',
            semesterSelectId: options.semesterSelectId || 'semesterSelect',
            courseSearchInputId: options.courseSearchInputId || 'courseSearch',
            courseSectionId: options.courseSectionId || 'courseSection',
            courseSelectionInfoId: options.courseSelectionInfoId || 'courseSelectionInfo',
            selectedCoursesSectionId: options.selectedCoursesSectionId || 'selectedCoursesSection',
            selectedCoursesListId: options.selectedCoursesListId || 'selectedCoursesList',
            selectedCountBadgeId: options.selectedCountBadgeId || 'selectedCountBadge',
            courseIdsInputId: options.courseIdsInputId || 'courseIdsInput',
            submitBtnId: options.submitBtnId || 'submitBtn',
            editMode: options.editMode || false,
            preSelectedCourses: options.preSelectedCourses || [],
            selectedProgram: '',
            selectedYear: '',
            selectedSemester: '',
            selectedCourses: [],
            courseSearchDropdown: null
        });
        
        this.init();
    }

    init() {
        this.$form = $(`#${this.formId}`);
        this.$programSelect = $(`#${this.programSelectId}`);
        this.$yearLevelSelect = $(`#${this.yearLevelSelectId}`);
        this.$semesterSelect = $(`#${this.semesterSelectId}`);
        this.$courseSection = $(`#${this.courseSectionId}`);
        this.$courseSelectionInfo = $(`#${this.courseSelectionInfoId}`);
        this.$selectedCoursesSection = $(`#${this.selectedCoursesSectionId}`);
        this.$selectedCoursesList = $(`#${this.selectedCoursesListId}`);
        this.$selectedCountBadge = $(`#${this.selectedCountBadgeId}`);
        this.$courseIdsInput = $(`#${this.courseIdsInputId}`);
        this.$submitBtn = $(`#${this.submitBtnId}`);

        if (this.editMode) {
            this.selectedCourses = this.preSelectedCourses.map(c => ({
                id: c.Id,
                code: c.CourseCode,
                title: c.CourseTitle,
                description: c.Description || ''
            }));
            
            if (this.$courseSelectionInfo && this.$courseSelectionInfo.length) {
                this.$courseSelectionInfo.hide();
            }
            
            if (this.$courseSection && this.$courseSection.length) {
                this.$courseSection.show();
            } else {
            }
            
            if (this.$selectedCoursesSection && this.$selectedCoursesSection.length) {
                this.$selectedCoursesSection.show();
            }
            
            this.initCourseSearch();
            
            this.updateSelectedCoursesDisplay();
            this.validateForm();
        } else {
            if (this.$courseSelectionInfo && this.$courseSelectionInfo.length) {
                this.$courseSelectionInfo.show();
            }
            this.attachEvents();
        }
        
        this.exposeGlobalFunctions();
    }

    attachEvents() {
        if (this.$programSelect.length && this.$yearLevelSelect.length && this.$semesterSelect.length) {
            $(`#${this.programSelectId}, #${this.yearLevelSelectId}, #${this.semesterSelectId}`)
                .on('change', () => this.handleCurriculumChange());
        }
    }

    handleCurriculumChange() {
        this.selectedProgram = this.$programSelect.val();
        this.selectedYear = this.$yearLevelSelect.val();
        this.selectedSemester = this.$semesterSelect.val();

        if (this.selectedProgram && this.selectedYear && this.selectedSemester) {
            this.$courseSelectionInfo.hide();
            this.$courseSection.slideDown(250);
            
            if (!this.courseSearchDropdown) {
                this.initCourseSearch();
            }
            
            $(`#${this.courseSearchInputId}`).focus();
        } else {
            this.resetCourseSection();
        }

        this.validateForm();
    }

    initCourseSearch() {
        this.courseSearchDropdown = new CourseSearchDropdown({
            searchInputId: this.courseSearchInputId,
            resultsDropdownId: 'courseSearchDropdown',
            resultsListId: 'courseSearchList',
            noResultsMessageId: 'courseSearchNoResults',
            loadingId: 'courseSearchLoading',
            resultsCountId: 'courseSearchCount',
            getExcludedIds: () => this.selectedCourses.map(c => c.id),
            onSelect: (course) => this.addCourse(course)
        });
    }

    resetCourseSection() {
        this.$courseSection.hide();
        this.$selectedCoursesSection.hide();
        this.$courseSelectionInfo.show();
        this.courseSearchDropdown?.clearSearch();
        this.selectedCourses = [];
        this.updateSelectedCoursesDisplay();
    }

    addCourse(course) {
        if (!this.selectedCourses.some(c => c.id === course.id)) {
            this.selectedCourses.push(course);
            this.updateSelectedCoursesDisplay();
            this.validateForm();
        }
    }

    removeCourse(id) {
        this.selectedCourses = this.selectedCourses.filter(c => c.id !== id);
        this.updateSelectedCoursesDisplay();
        this.validateForm();
    }

    clearAllSelections() {
        this.selectedCourses = [];
        this.updateSelectedCoursesDisplay();
        this.validateForm();
    }

    updateSelectedCoursesDisplay() {
        if (this.selectedCourses.length === 0) {
            this.$selectedCoursesSection.show();
            this.$selectedCountBadge.text(0);
            this.$selectedCoursesList.html('<div class="no-selected-items"><i class="fas fa-folder-open"></i><span>No courses selected</span></div>');
            this.$courseIdsInput.val('');
            return;
        }

        // Render selected courses
        this.$selectedCoursesSection.show();
        this.$selectedCountBadge.text(this.selectedCourses.length);

        const html = this.selectedCourses.map(c => `
        <div class="selected-item" data-course-id="${c.id}">
            <div class="selected-item-main">
                <div class="selected-item-header">
                    <span class="selected-item-code">${c.code}</span>
                    <span class="selected-item-title">${c.title}</span>
                </div>
                ${c.description ? `<div class="selected-item-description">${c.description}</div>` : ''}
            </div>
            <button type="button" class="remove-item-btn" onclick="removeCourseFromSelection(${c.id})">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `).join('');

        this.$selectedCoursesList.html(html);
        this.$courseIdsInput.val(this.selectedCourses.map(c => c.id).join(','));
    }


    validateForm() {
        let valid;
        if (this.editMode) {
            valid = this.selectedCourses.length > 0;
        } else {
            valid = this.selectedProgram && this.selectedYear && this.selectedSemester && this.selectedCourses.length > 0;
        }
        this.$submitBtn.prop('disabled', !valid);
    }

    exposeGlobalFunctions() {
        window.clearAllSelections = () => this.clearAllSelections();
        window.removeCourse = (id) => this.removeCourse(id);
        window.removeCourseFromSelection = (id) => this.removeCourse(id);
    }
}

Object.assign(window, {
    SearchableDropdown,
    CourseSearchDropdown,
    TeacherSearchDropdown,
    StudentSearchDropdown,
    CourseAssignmentForm
});
