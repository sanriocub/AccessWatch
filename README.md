# AccessWatch — Accessibility Issue Reporting and Resolution Platform

CT071-3-3-DDAC Group Project (Task 1)

---

## 🛠 Step 1: Install Git

Download and install Git if you haven't already:
👉 https://git-scm.com/downloads

---

## 📥 Step 2: Clone the Project

1. Open **Visual Studio**
2. On the start window, click **"Clone a repository"**
   (Or: **File → Clone Repository** if you already have a project open)
3. Paste this link:
   `https://github.com/sanriocub/AccessWatch.git`
4. Choose where to save it on your machine, then click **Clone**
5. Visual Studio will load the solution automatically — open `AccessWatch.sln` if it doesn't open by itself

**Before you run it the first time:**
- Restore NuGet packages: right-click the solution in **Solution Explorer → Restore NuGet Packages**
- Update your local `appsettings.json` connection string to point at your own local database (don't copy anyone else's — see note below)

---

## 🗂 Project Folder Structure

```
AccessWatch/
└── AccessWatch/
    ├── Controllers/
    │   ├── AccountController.cs      → register / login
    │   ├── ReportController.cs       → submit, track, view reports (Person with Disability)
    │   ├── AdminController.cs        → manage accounts, review/assign reports (Platform Administrator)
    │   ├── InspectionController.cs   → view cases, findings, ratings (Accessibility Inspector)
    │   ├── FacilityController.cs     → repair tasks, progress, completion (Facility Maintenance Officer)
    │   └── HomeController.cs         → landing page
    ├── Models/
    │   ├── User.cs
    │   ├── AccessibilityReport.cs    → shared report record — touched by all 4 roles
    │   ├── Inspection.cs
    │   ├── Repair.cs
    │   ├── Category.cs
    │   ├── Facility.cs
    │   ├── Notification.cs
    │   └── AccessWatchDbContext.cs
    ├── Views/
    │   ├── Account/
    │   ├── Report/
    │   ├── Admin/
    │   ├── Inspection/
    │   ├── Facility/
    │   ├── Home/
    │   └── Shared/
    ├── wwwroot/                      → css, js, images
    └── Program.cs
```

---

## 👥 Roles & Responsibilities

| Role | Person | Task Summary |
|---|---|---|
| Person with Disability | Abdullah Omar Yusuf | Register/login, update profile, submit accessibility reports with images, track report status |
| Platform Administrator | Sangeetha Rajsubramanian | Manage user accounts, review/approve/reject reports, assign inspectors, manage categories, monitor activity |
| Accessibility Inspector | Omer Abdulaziz Ali Dahesh | View assigned cases, submit inspection findings and ratings, recommend corrective action, forward to maintenance |
| Facility Maintenance Officer | Qusai Nasr Mohammed | View repair tasks, update repair progress, record corrective actions, mark repairs complete |

**Work only inside your own controller/views folder, and don't edit someone else's role's code without checking with them first.**

The one shared file everyone touches is `AccessibilityReport.cs` (the model) — don't change its fields without telling the group first, since it'll break other people's queries.

---

## 🔄 How to Push & Pull Code

You can do this either with the buttons in Visual Studio, or with the terminal commands — both do the same thing.

**🟢 Every time before you start working — pull first:**
- **VS button:** open **Git** panel --> press **Pull**

**OR**

- **Terminal:** `git pull origin main`



**🛑 Always pull before pushing.** If you skip this, it could cause conflicts and overwrite someone else's work.

**When you're done with a change — commit and push:** everytime when youre done editing
- **VS button:** open **Git** panel 
1. press commit or stash
2. write whate you edited
3. press commit all
4. sync


**OR**

- **Terminal:**
  ```
  git add .
  git commit -m "Add [what u did]"
  git push origin main
  ```

**If you get a merge conflict:** stop, don't force-push, and message the group — conflicts in `AccessibilityReport.cs` or `Program.cs` usually mean two people changed the same shared file at once. Visual Studio will show conflicted files with a warning icon in the Git Changes panel and let you open a merge editor to resolve them line by line.

---

## ☁️ Deployment Notes

- Final demo must run on **AWS** (Elastic Beanstalk) — **not** `localhost`
- Database is **Amazon RDS**, no Lambda
- Never commit real RDS credentials to this repo — keep your local `appsettings.json` connection string pointing at your own local DB, and set the real RDS connection string as an environment variable in Elastic Beanstalk instead
