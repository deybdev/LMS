-- ----------------------------
-- 15 Random Users (Teachers & Students)
-- ----------------------------

-- Teachers
INSERT INTO [dbo].[Users] ([UserID],[FirstName],[LastName],[Email],[PhoneNumber],[Department],[Role],[Password],[LastLogin],[DateCreated])
VALUES
('TCH003','Sophia','Reyes','sophia.reyes@example.com','09170001111','Physics','Teacher','teach123',NULL,GETDATE()),
('TCH004','Miguel','Torres','miguel.torres@example.com','09170002222','Chemistry','Teacher','teach123',NULL,GETDATE()),
('TCH005','Isabel','Cruz','isabel.cruz@example.com','09170003333','Mathematics','Teacher','teach123',NULL,GETDATE()),
('TCH006','Carlos','Flores','carlos.flores@example.com','09170004444','Biology','Teacher','teach123',NULL,GETDATE()),
('TCH007','Ana','Lopez','ana.lopez@example.com','09170005555','English','Teacher','teach123',NULL,GETDATE());

-- Students
INSERT INTO [dbo].[Users] ([UserID],[FirstName],[LastName],[Email],[PhoneNumber],[Department],[Role],[Password],[LastLogin],[DateCreated])
VALUES
('STD003','Lucas','Dela Cruz','lucas.delacruz@example.com','09990001111','Information Technology','Student','stud123',NULL,GETDATE()),
('STD004','Emma','Gonzales','emma.gonzales@example.com','09990002222','Computer Science','Student','stud123',NULL,GETDATE()),
('STD005','Noah','Santos','noah.santos@example.com','09990003333','Information Technology','Student','stud123',NULL,GETDATE()),
('STD006','Olivia','Reyes','olivia.reyes@example.com','09990004444','Computer Science','Student','stud123',NULL,GETDATE()),
('STD007','Ethan','Garcia','ethan.garcia@example.com','09990005555','Information Technology','Student','stud123',NULL,GETDATE()),
('STD008','Mia','Torres','mia.torres@example.com','09990006666','Computer Science','Student','stud123',NULL,GETDATE()),
('STD009','Aiden','Cruz','aiden.cruz@example.com','09990007777','Information Technology','Student','stud123',NULL,GETDATE()),
('STD010','Sophia','Flores','sophia.flores@example.com','09990008888','Computer Science','Student','stud123',NULL,GETDATE()),
('STD011','Liam','Lopez','liam.lopez@example.com','09990009999','Information Technology','Student','stud123',NULL,GETDATE()),
('STD012','Isabella','Gomez','isabella.gomez@example.com','09990010000','Computer Science','Student','stud123',NULL,GETDATE());
