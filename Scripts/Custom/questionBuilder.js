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

// Add a new question manually
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
    
    // Scroll to the newly added question
    setTimeout(() => {
        const questionCards = document.querySelectorAll('.question-card');
        if (questionCards.length > 0) {
            questionCards[questionCards.length - 1].scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
    }, 100);
}

// CSV Import Function - APPENDS to existing questions
function importQuestionsFromCSV() {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.csv';
    
    input.onchange = function(e) {
        const file = e.target.files[0];
        if (!file) return;
        
        const reader = new FileReader();
        reader.onload = function(event) {
            try {
                parseCSVAndAddQuestions(event.target.result);
            } catch (error) {
                alert('Error parsing CSV file: ' + error.message);
                console.error('CSV Parse Error:', error);
            }
        };
        reader.readAsText(file);
    };
    
    input.click();
}

// Parse CSV and ADD questions to existing ones
function parseCSVAndAddQuestions(csvText) {
    const lines = csvText.split('\n').filter(line => line.trim() !== '');
    
    if (lines.length < 2) {
        alert('CSV file is empty or invalid');
        return;
    }
    
    // Parse header
    const headers = parseCSVLine(lines[0]).map(h => h.trim().toLowerCase());
    
    // Expected headers
    const requiredHeaders = ['type', 'question', 'points'];
    const missingHeaders = requiredHeaders.filter(h => !headers.includes(h));
    
    if (missingHeaders.length > 0) {
        alert('CSV missing required columns: ' + missingHeaders.join(', ') + '\n\nRequired columns: type, question, points\nOptional columns: option1, option2, option3, option4, option5, correct_answer, case_sensitive, max_words');
        return;
    }
    
    let importedCount = 0;
    let errorCount = 0;
    const errors = [];
    const startingQuestionCount = questions.length;
    
    // Parse data rows and ADD to existing questions
    for (let i = 1; i < lines.length; i++) {
        const values = parseCSVLine(lines[i]);
        
        if (values.length === 0 || values.every(v => v.trim() === '')) {
            continue; // Skip empty lines
        }
        
        try {
            const row = {};
            headers.forEach((header, index) => {
                row[header] = values[index] ? values[index].trim() : '';
            });
            
            const question = createQuestionFromCSVRow(row, i + 1);
            if (question) {
                questionCounter++;
                question.id = questionCounter;
                questions.push(question); // APPEND to existing questions
                importedCount++;
            }
        } catch (error) {
            errorCount++;
            errors.push(`Row ${i + 1}: ${error.message}`);
        }
    }
    
    renderQuestions();
    
    // Build success message
    let message = `✅ Successfully imported ${importedCount} question(s)`;
    
    if (startingQuestionCount > 0) {
        message += `\n\n📝 Total questions now: ${questions.length} (${startingQuestionCount} existing + ${importedCount} imported)`;
    }
    
    if (errorCount > 0) {
        message += `\n\n❌ Errors: ${errorCount}\n${errors.slice(0, 5).join('\n')}`;
        if (errors.length > 5) {
            message += `\n... and ${errors.length - 5} more errors`;
        }
    }
    
    message += '\n\n💡 Tip: You can continue adding questions manually or import more CSV files!';
    
    alert(message);
    
    // Scroll to show the newly imported questions
    if (importedCount > 0) {
        setTimeout(() => {
            const questionCards = document.querySelectorAll('.question-card');
            if (questionCards.length > startingQuestionCount) {
                questionCards[startingQuestionCount].scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        }, 300);
    }
}

// Parse CSV line (handles quotes and commas)
function parseCSVLine(line) {
    const result = [];
    let current = '';
    let inQuotes = false;
    
    for (let i = 0; i < line.length; i++) {
        const char = line[i];
        
        if (char === '"') {
            if (inQuotes && line[i + 1] === '"') {
                current += '"';
                i++;
            } else {
                inQuotes = !inQuotes;
            }
        } else if (char === ',' && !inQuotes) {
            result.push(current);
            current = '';
        } else {
            current += char;
        }
    }
    
    result.push(current);
    return result;
}

// Create question object from CSV row
function createQuestionFromCSVRow(row, rowNumber) {
    const type = row.type.toLowerCase();
    const questionText = row.question;
    const points = parseInt(row.points) || 1;
    
    if (!questionText) {
        throw new Error('Question text is required');
    }
    
    const question = {
        type: '',
        questionText: questionText,
        points: points,
        required: true
    };
    
    // Map CSV type to internal type
    const typeMap = {
        'multiple choice': 'multipleChoice',
        'multiplechoice': 'multipleChoice',
        'mc': 'multipleChoice',
        'multiple answer': 'multipleAnswer',
        'multipleanswer': 'multipleAnswer',
        'ma': 'multipleAnswer',
        'true/false': 'trueFalse',
        'truefalse': 'trueFalse',
        'tf': 'trueFalse',
        'identification': 'identification',
        'ident': 'identification',
        'essay': 'essay',
        'text': 'essay',
        'file upload': 'fileUpload',
        'fileupload': 'fileUpload',
        'file': 'fileUpload'
    };
    
    question.type = typeMap[type];
    
    if (!question.type) {
        throw new Error(`Invalid question type: ${row.type}. Valid types: multiple choice, multiple answer, true/false, identification, essay, file upload`);
    }
    
    // Handle type-specific fields
    if (question.type === 'multipleChoice' || question.type === 'multipleAnswer') {
        const options = [];
        const correctAnswers = (row.correct_answer || '').split('|').map(a => a.trim().toUpperCase());
        
        // Collect options from option1, option2, etc.
        for (let i = 1; i <= 10; i++) {
            const optionKey = 'option' + i;
            if (row[optionKey] && row[optionKey].trim() !== '') {
                const optionLetter = String.fromCharCode(64 + i); // A, B, C, D, etc.
                options.push({
                    text: row[optionKey].trim(),
                    isCorrect: correctAnswers.includes(optionLetter)
                });
            }
        }
        
        if (options.length < 2) {
            throw new Error('Multiple choice/answer questions must have at least 2 options');
        }
        
        if (!options.some(opt => opt.isCorrect)) {
            throw new Error('At least one option must be marked as correct');
        }
        
        question.options = options;
        
    } else if (question.type === 'trueFalse') {
        const answer = (row.correct_answer || '').trim().toLowerCase();
        if (answer === 'true' || answer === 't' || answer === '1') {
            question.correctAnswer = true;
        } else if (answer === 'false' || answer === 'f' || answer === '0') {
            question.correctAnswer = false;
        } else {
            throw new Error('True/False question must have correct_answer as "true" or "false"');
        }
        
    } else if (question.type === 'identification') {
        if (!row.correct_answer || row.correct_answer.trim() === '') {
            throw new Error('Identification question must have a correct_answer');
        }
        question.correctAnswer = row.correct_answer.trim();
        question.caseSensitive = (row.case_sensitive || '').toLowerCase() === 'true' || (row.case_sensitive || '').toLowerCase() === 'yes';
        
    } else if (question.type === 'essay') {
        question.maxWords = row.max_words ? parseInt(row.max_words) : null;
        
    } else if (question.type === 'fileUpload') {
        question.allowedFileTypes = row.allowed_file_types || '.pdf,.doc,.docx';
        question.maxFileSize = row.max_file_size ? parseInt(row.max_file_size) : 10;
    }
    
    return question;
}

// Download CSV Template
function downloadCSVTemplate() {
    const template = `type,question,points,option1,option2,option3,option4,option5,correct_answer,case_sensitive,max_words,allowed_file_types,max_file_size
multiple choice,"What is 2 + 2?",1,2,3,4,5,,C,,,
multiple answer,"Select all prime numbers",2,1,2,3,4,,B|C,,,
true/false,"The Earth is flat",1,,,,,,false,,,
identification,"Capital of France",1,,,,,,Paris,yes,,
essay,"Explain photosynthesis",5,,,,,,,,,200,
file upload,"Upload your assignment",10,,,,,,,,,.pdf|.docx,5`;
    
    const blob = new Blob([template], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'questions_template.csv';
    a.click();
    window.URL.revokeObjectURL(url);
}

// Render all questions
function renderQuestions() {
    const container = $('#questionsList');
    container.empty();

    if (questions.length === 0) {
        container.html(`
            <div class="empty-questions">
                <i class="fas fa-clipboard-question"></i>
                <p>No questions added yet. Click "Add Question" above to create questions manually or "Import CSV" to bulk import.</p>
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
                    <button type="button" class="btn-question-action btn-delete-question" onclick="deleteQuestion(${index})" title="Delete question">
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
    if (property === 'points') {
        updateTotalPoints();
    }
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
            <strong style="color: var(--primary-color);">Total: ${questions.length} question${questions.length !== 1 ? 's' : ''} • ${total} point${total !== 1 ? 's' : ''}</strong>
        `);
    } else {
        headerText.text('Create questions for students to answer. Points will be calculated from questions.');
    }
}
