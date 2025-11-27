// Question Builder JavaScript
let questions = [];
let questionCounter = 0;
let lastSelectedType = ''; // Remember last selected question type

$(document).ready(function () {
    // Show submission mode row when classwork type is selected
    $('#classworkType').on('change', function () {
        const type = $(this).val();
        
        if (type) {
            $('#submissionModeRow').slideDown();
            // Trigger mode change to show appropriate sections
            $('input[name="submissionMode"]:checked').trigger('change');
        } else {
            $('#submissionModeRow').slideUp();
            $('#questionsSection').slideUp();
            $('#fileAttachmentSection').slideUp();
            $('#pointsGroup').show();
        }
    });

    // Handle submission mode change
    $('input[name="submissionMode"]').on('change', function () {
        const mode = $(this).val();
        const classworkType = $('#classworkType').val();
        
        if (!classworkType) return;

        if (mode === 'manual') {
            // Manual mode: Show questions, hide file attachments, hide points input
            $('#questionsSection').slideDown();
            $('#fileAttachmentSection').slideUp();
            $('#pointsGroup').slideUp();
            
            // Clear points input and make it not required
            $('#totalPoints').removeAttr('required').val('');
            
        } else if (mode === 'file') {
            // File mode: Hide questions, show file attachments, show points input
            $('#questionsSection').slideUp();
            $('#fileAttachmentSection').slideDown();
            $('#pointsGroup').slideDown();
            
            // Make points required again
            $('#totalPoints').attr('required', 'required');
            
            // Clear questions
            questions = [];
            questionCounter = 0;
            renderQuestions();
        }
    });

    // Form submission handler
    $('#classworkForm').on('submit', function (e) {
        const mode = $('input[name="submissionMode"]:checked').val();
        const type = $('#classworkType').val();
        
        if (mode === 'manual') {
            // Manual mode validation
            if (questions.length === 0) {
                e.preventDefault();
                alert('Please add at least one question for manual submission mode');
                return false;
            }

            // Validate that all questions have required fields
            for (let i = 0; i < questions.length; i++) {
                const q = questions[i];
                
                if (!q.questionText || q.questionText.trim() === '') {
                    e.preventDefault();
                    alert('Question ' + (i + 1) + ' is missing question text');
                    return false;
                }

                if (!q.points || q.points <= 0) {
                    e.preventDefault();
                    alert('Question ' + (i + 1) + ' must have points greater than 0');
                    return false;
                }

                if (q.type === 'multipleChoice' || q.type === 'multipleAnswer') {
                    if (!q.options || q.options.length < 2) {
                        e.preventDefault();
                        alert('Question ' + (i + 1) + ' must have at least 2 options');
                        return false;
                    }

                    const hasCorrect = q.options.some(opt => opt.isCorrect);
                    if (!hasCorrect) {
                        e.preventDefault();
                        alert('Question ' + (i + 1) + ' must have at least one correct answer selected');
                        return false;
                    }
                }

                if (q.type === 'trueFalse') {
                    if (q.correctAnswer === undefined || q.correctAnswer === null) {
                        e.preventDefault();
                        alert('Question ' + (i + 1) + ' must have a correct answer selected');
                        return false;
                    }
                }

                if (q.type === 'identification') {
                    if (!q.correctAnswer || q.correctAnswer.trim() === '') {
                        e.preventDefault();
                        alert('Question ' + (i + 1) + ' must have a correct answer');
                        return false;
                    }
                }
            }

            // Calculate total points from questions and set it
            const totalPoints = questions.reduce((sum, q) => sum + (q.points || 0), 0);
            $('#totalPoints').val(totalPoints);
            
        } else if (mode === 'file') {
            // File mode validation
            const points = parseInt($('#totalPoints').val());
            if (!points || points <= 0) {
                e.preventDefault();
                alert('Please specify points for this classwork');
                return false;
            }
            
            // Clear questions data for file mode
            $('#questionsData').val('');
        }

        // Serialize questions to JSON for manual mode
        if (mode === 'manual') {
            $('#questionsData').val(JSON.stringify(questions));
        }
    });
});

// Add a new question
function addQuestion() {
    const type = $('#questionTypeSelect').val();
    
    if (!type) {
        alert('Please select a question type');
        return;
    }

    // Remember the last selected type
    lastSelectedType = type;

    questionCounter++;
    
    const question = {
        id: questionCounter,
        type: type,
        questionText: '',
        points: 1,
        required: true
    };

    if (type === 'multipleChoice') {
        question.options = [
            { text: '', isCorrect: false },
            { text: '', isCorrect: false }
        ];
    } else if (type === 'multipleAnswer') {
        question.options = [
            { text: '', isCorrect: false },
            { text: '', isCorrect: false }
        ];
    } else if (type === 'trueFalse') {
        question.correctAnswer = null; // true or false
    } else if (type === 'identification') {
        question.correctAnswer = '';
        question.caseSensitive = false;
    } else if (type === 'essay') {
        question.maxWords = null;
    } else if (type === 'fileUpload') {
        question.allowedFileTypes = '.pdf,.doc,.docx';
        question.maxFileSize = 10; // MB
    }

    questions.push(question);
    renderQuestions();
    
    // DON'T reset selector - keep the last selected type
    // The selector will automatically maintain the selected value
}

// Render all questions
function renderQuestions() {
    const container = $('#questionsList');
    container.empty();

    if (questions.length === 0) {
        container.html(`
            <div class="empty-questions">
                <i class="fas fa-clipboard-question"></i>
                <p>No questions added yet. Click "Add Question" above to create questions.</p>
                <small style="color: #999; margin-top: 10px; display: block;">
                    At least one question is required for manual submission mode.
                </small>
            </div>
        `);
        return;
    }

    questions.forEach((question, index) => {
        const card = createQuestionCard(question, index);
        container.append(card);
    });

    // Move the question type selector below the last question
    moveQuestionSelector();
    
    updateTotalPoints();
}

// Move question type selector below the last added question
function moveQuestionSelector() {
    const selector = $('.question-type-selector').parent();
    const sectionHeader = $('.section-header');
    
    if (questions.length > 0) {
        // Move selector after the questions list
        selector.insertAfter('#questionsList');
        sectionHeader.find('h3, p').show();
        
        // Keep the last selected type
        if (lastSelectedType) {
            $('#questionTypeSelect').val(lastSelectedType);
        }
    } else {
        // Move it back to header if no questions
        selector.appendTo(sectionHeader);
    }
}

// Create question card HTML
function createQuestionCard(question, index) {
    const typeLabels = {
        multipleChoice: 'Multiple Choice',
        multipleAnswer: 'Multiple Answer',
        trueFalse: 'True/False',
        identification: 'Identification',
        essay: 'Essay/Text',
        fileUpload: 'File Upload'
    };

    let specificFields = '';

    if (question.type === 'multipleChoice') {
        specificFields = createMultipleChoiceFields(question, index);
    } else if (question.type === 'multipleAnswer') {
        specificFields = createMultipleAnswerFields(question, index);
    } else if (question.type === 'trueFalse') {
        specificFields = createTrueFalseFields(question, index);
    } else if (question.type === 'identification') {
        specificFields = createIdentificationFields(question, index);
    } else if (question.type === 'essay') {
        specificFields = createEssayFields(question, index);
    } else if (question.type === 'fileUpload') {
        specificFields = createFileUploadFields(question, index);
    }

    return `
        <div class="question-card ${question.type}" data-index="${index}">
            <div class="question-header">
                <div>
                    <span class="question-number">Question ${index + 1}</span>
                    <span class="question-type-badge ${question.type}">${typeLabels[question.type]}</span>
                </div>
                <div class="question-actions">
                    <button type="button" class="btn-question-action btn-delete-question" onclick="deleteQuestion(${index})">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>

            <div class="question-input-group">
                <label>Question Text <span class="required">*</span></label>
                <textarea placeholder="Enter your question here..." 
                          onchange="updateQuestion(${index}, 'questionText', this.value)">${question.questionText}</textarea>
            </div>

            <div class="question-input-group points-input">
                <label>Points <span class="required">*</span></label>
                <input type="number" min="1" value="${question.points}" 
                       onchange="updateQuestion(${index}, 'points', parseInt(this.value))" />
            </div>

            ${specificFields}
        </div>
    `;
}

// Create multiple choice specific fields
function createMultipleChoiceFields(question, qIndex) {
    let optionsHTML = question.options.map((option, oIndex) => `
        <div class="option-item ${option.isCorrect ? 'correct' : ''}">
            <input type="radio" 
                   name="correct_${qIndex}" 
                   ${option.isCorrect ? 'checked' : ''} 
                   onchange="setCorrectAnswer(${qIndex}, ${oIndex})"
                   title="Mark as correct answer" />
            <input type="text" 
                   placeholder="Option ${oIndex + 1}" 
                   value="${option.text}"
                   onchange="updateOption(${qIndex}, ${oIndex}, this.value)" />
            ${question.options.length > 2 ? `
                <button type="button" class="btn-remove-option" onclick="removeOption(${qIndex}, ${oIndex})">
                    <i class="fas fa-times"></i>
                </button>
            ` : ''}
        </div>
    `).join('');

    return `
        <div class="question-input-group">
            <label>Answer Options <span class="required">*</span></label>
            <small style="color: #666; display: block; margin-bottom: 10px;">
                Select the radio button to mark the correct answer (single choice)
            </small>
            <div class="options-list">
                ${optionsHTML}
            </div>
            <button type="button" class="btn-add-option" onclick="addOption(${qIndex})">
                <i class="fas fa-plus"></i> Add Option
            </button>
        </div>
    `;
}

// Create multiple answer specific fields
function createMultipleAnswerFields(question, qIndex) {
    let optionsHTML = question.options.map((option, oIndex) => `
        <div class="option-item ${option.isCorrect ? 'correct' : ''}">
            <input type="checkbox" 
                   ${option.isCorrect ? 'checked' : ''} 
                   onchange="toggleMultipleAnswer(${qIndex}, ${oIndex})"
                   title="Mark as correct answer" />
            <input type="text" 
                   placeholder="Option ${oIndex + 1}" 
                   value="${option.text}"
                   onchange="updateOption(${qIndex}, ${oIndex}, this.value)" />
            ${question.options.length > 2 ? `
                <button type="button" class="btn-remove-option" onclick="removeOption(${qIndex}, ${oIndex})">
                    <i class="fas fa-times"></i>
                </button>
            ` : ''}
        </div>
    `).join('');

    return `
        <div class="question-input-group">
            <label>Answer Options <span class="required">*</span></label>
            <small style="color: #666; display: block; margin-bottom: 10px;">
                Select checkboxes to mark correct answers (can select multiple)
            </small>
            <div class="options-list">
                ${optionsHTML}
            </div>
            <button type="button" class="btn-add-option" onclick="addOption(${qIndex})">
                <i class="fas fa-plus"></i> Add Option
            </button>
        </div>
    `;
}

// Create true/false specific fields
function createTrueFalseFields(question, index) {
    return `
        <div class="question-input-group">
            <label>Correct Answer <span class="required">*</span></label>
            <div class="true-false-options">
                <label class="tf-option ${question.correctAnswer === true ? 'selected' : ''}" onclick="updateQuestion(${index}, 'correctAnswer', true); renderQuestions();">
                    <input type="radio" 
                           name="tf_${index}" 
                           value="true"
                           ${question.correctAnswer === true ? 'checked' : ''}
                           onchange="updateQuestion(${index}, 'correctAnswer', true)" />
                    <span class="tf-label">
                        <i class="fas fa-check"></i> True
                    </span>
                </label>
                <label class="tf-option ${question.correctAnswer === false ? 'selected' : ''}" onclick="updateQuestion(${index}, 'correctAnswer', false); renderQuestions();">
                    <input type="radio" 
                           name="tf_${index}" 
                           value="false"
                           ${question.correctAnswer === false ? 'checked' : ''}
                           onchange="updateQuestion(${index}, 'correctAnswer', false)" />
                    <span class="tf-label">
                        <i class="fas fa-times"></i> False
                    </span>
                </label>
            </div>
        </div>
    `;
}

// Create identification specific fields
function createIdentificationFields(question, index) {
    return `
        <div class="question-input-group">
            <label>Correct Answer <span class="required">*</span></label>
            <input type="text" 
                   placeholder="Enter the correct answer" 
                   value="${question.correctAnswer || ''}"
                   onchange="updateQuestion(${index}, 'correctAnswer', this.value)" />
            <label style="margin-top: 8px; font-weight: normal; cursor: pointer;">
                <input type="checkbox" 
                       ${question.caseSensitive ? 'checked' : ''}
                       onchange="updateQuestion(${index}, 'caseSensitive', this.checked)" />
                Case sensitive
            </label>
            <small style="color: #666; margin-top: 5px; display: block;">
                Students must type the exact answer. Enable case sensitive if letter casing matters.
            </small>
        </div>
    `;
}

// Create essay specific fields
function createEssayFields(question, index) {
    return `
        <div class="question-input-group">
            <label>Maximum Words (Optional)</label>
            <input type="number" 
                   min="0" 
                   placeholder="Leave empty for no limit" 
                   value="${question.maxWords || ''}"
                   onchange="updateQuestion(${index}, 'maxWords', parseInt(this.value) || null)" />
            <small style="color: #666; margin-top: 5px; display: block;">
                Students will type their answer in a text area
            </small>
        </div>
    `;
}

// Create file upload specific fields
function createFileUploadFields(question, index) {
    return `
        <div class="question-input-group">
            <label>Allowed File Types</label>
            <input type="text" 
                   value="${question.allowedFileTypes}"
                   onchange="updateQuestion(${index}, 'allowedFileTypes', this.value)"
                   placeholder=".pdf,.doc,.docx" />
            <small style="color: #666; margin-top: 5px; display: block;">
                Enter file extensions separated by commas
            </small>
        </div>
        <div class="question-input-group">
            <label>Maximum File Size (MB)</label>
            <input type="number" 
                   min="1" 
                   max="50" 
                   value="${question.maxFileSize}"
                   onchange="updateQuestion(${index}, 'maxFileSize', parseInt(this.value))" />
        </div>
    `;
}

// Update question property
function updateQuestion(index, property, value) {
    questions[index][property] = value;
}

// Update option text
function updateOption(qIndex, oIndex, value) {
    questions[qIndex].options[oIndex].text = value;
}

// Set correct answer for multiple choice (single selection)
function setCorrectAnswer(qIndex, oIndex) {
    questions[qIndex].options.forEach((opt, i) => {
        opt.isCorrect = (i === oIndex);
    });
    renderQuestions();
}

// Toggle correct answer for multiple answer (multiple selections)
function toggleMultipleAnswer(qIndex, oIndex) {
    questions[qIndex].options[oIndex].isCorrect = !questions[qIndex].options[oIndex].isCorrect;
    renderQuestions();
}

// Add option to multiple choice/answer
function addOption(qIndex) {
    questions[qIndex].options.push({
        text: '',
        isCorrect: false
    });
    renderQuestions();
}

// Remove option from multiple choice/answer
function removeOption(qIndex, oIndex) {
    if (questions[qIndex].options.length <= 2) {
        alert('A question must have at least 2 options');
        return;
    }

    questions[qIndex].options.splice(oIndex, 1);
    renderQuestions();
}

// Delete question
function deleteQuestion(index) {
    if (confirm('Are you sure you want to delete this question?')) {
        questions.splice(index, 1);
        renderQuestions();
    }
}

// Update total points display
function updateTotalPoints() {
    const total = questions.reduce((sum, q) => sum + (q.points || 0), 0);
    
    // Update the section header to show total points
    const headerText = $('#questionsSection .section-header p');
    if (questions.length > 0) {
        headerText.html(`
            Create questions for students to answer. 
            <strong style="color: var(--primary-color);">Total Points: ${total}</strong>
        `);
    } else {
        headerText.text('Create questions for students to answer. Points will be calculated from questions.');
    }
}
