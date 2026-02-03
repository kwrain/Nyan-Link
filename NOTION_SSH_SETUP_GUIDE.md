# 🔐 SSH 키 설정 가이드 - Unity 비공개 패키지 설치

> **목적**: Unity 프로젝트에서 비공개 GitHub 저장소(`NineMood.Services`) 패키지를 사용하기 위해 SSH 키를 설정하는 방법입니다.

---

## 🚀 빠른 시작 가이드

### 🖱️ GUI 도구 사용자 (GitHub Desktop) - 간단한 방법

1. ✅ **GitHub Desktop 설치 및 로그인** → SSH 키 자동 생성 및 추가 완료
2. 🧪 **SSH 연결 테스트** (선택사항) → [4단계](#4단계-ssh-연결-테스트)
3. 📦 **Unity에서 패키지 설치** → [5단계](#5단계-unity-프로젝트에서-패키지-설치)

> ⏭️ **2단계와 3단계는 건너뛰어도 됩니다!** GitHub Desktop이 자동으로 처리합니다.

### ⌨️ 터미널 사용자 - 전체 단계 진행

1. SSH 키 확인 및 생성
2. SSH 공개 키 복사
3. GitHub에 SSH 키 추가
4. SSH 연결 테스트
5. Unity에서 패키지 설치

---

## 📋 목차

- [1단계: SSH 키 확인 및 생성](#1단계-ssh-키-확인-및-생성)
- [2단계: SSH 공개 키 복사](#2단계-ssh-공개-키-복사)
- [3단계: GitHub에 SSH 키 추가](#3단계-github에-ssh-키-추가)
- [4단계: SSH 연결 테스트](#4단계-ssh-연결-테스트)
- [5단계: Unity 프로젝트에서 패키지 설치](#5단계-unity-프로젝트에서-패키지-설치)
- [문제 해결](#문제-해결)
- [자주 묻는 질문](#자주-묻는-질문)

---

## 1단계: SSH 키 확인 및 생성

### ✅ SSH 키 확인

터미널(또는 Git Bash)에서 다음 명령어를 실행하여 이미 SSH 키가 있는지 확인합니다:

```bash
ls -la ~/.ssh/id_*.pub
```

**결과:**
- ✅ 파일이 있으면 → [2단계로 이동](#2단계-ssh-공개-키-복사)
- ❌ 파일이 없으면 → 아래 키 생성 단계 진행

### 🔑 SSH 키 생성

> ⚠️ **중요**: SSH 키는 **반드시 로컬 컴퓨터에서 생성**해야 합니다. GitHub 웹사이트에서는 SSH 키를 생성할 수 없습니다. 이는 보안상의 이유 때문입니다.

SSH 키를 생성하는 방법은 두 가지가 있습니다:

#### 방법 1: GUI 도구 사용 (초보자 권장) 🖱️

터미널 명령어가 어렵다면 GUI 도구를 사용할 수 있습니다:

##### GitHub Desktop 사용

1. **GitHub Desktop 설치**
   - [desktop.github.com](https://desktop.github.com)에서 다운로드 및 설치

2. **GitHub Desktop 실행**
   - **File** → **Options** (또는 **Preferences**)
   - **Accounts** 탭에서 GitHub 계정 로그인
   - GitHub Desktop이 **자동으로 SSH 키를 생성하고 GitHub에 추가**합니다

3. **완료 확인**
   - ✅ SSH 키 생성: 자동 처리됨
   - ✅ 공개 키 복사: 불필요 (자동 처리됨)
   - ✅ GitHub에 키 추가: 자동 처리됨
   - → **2단계와 3단계를 건너뛰고 [4단계로 이동](#4단계-ssh-연결-테스트)하세요**

> 💡 생성된 키는 `~/.ssh/id_rsa.pub` 또는 `~/.ssh/id_ed25519.pub`에 저장됩니다 (확인용)

##### SourceTree 사용

1. **SourceTree 설치**
   - [sourcetreeapp.com](https://www.sourcetreeapp.com)에서 다운로드 및 설치

2. **SSH 키 생성**
   - **Tools** → **Create or Import SSH Keys**
   - **Generate** 버튼 클릭
   - 키 생성 후 **Save public key** 클릭하여 공개 키 저장

##### PuTTYgen 사용 (Windows)

1. **PuTTYgen 다운로드**
   - [putty.org](https://www.putty.org)에서 PuTTY 다운로드

2. **키 생성**
   - PuTTYgen 실행
   - **Generate** 버튼 클릭
   - 마우스를 빈 공간에서 움직여 랜덤 데이터 생성
   - **Save public key** 및 **Save private key** 클릭하여 저장

> 💡 **참고**: GUI 도구를 사용해도 최종적으로는 로컬 컴퓨터에 키가 생성됩니다. 웹사이트에서 직접 생성하는 것은 불가능합니다.

#### 방법 2: 터미널 명령어 사용 (고급 사용자) ⌨️

##### macOS / Linux 사용자

**Ed25519 키 (권장):**
```bash
ssh-keygen -t ed25519 -C "your_email@example.com"
```

**RSA 키 (대안):**
```bash
ssh-keygen -t rsa -b 4096 -C "your_email@example.com"
```

##### Windows 사용자

**Git Bash 사용:**
```bash
ssh-keygen -t ed25519 -C "your_email@example.com"
```

**PowerShell 사용:**
```powershell
ssh-keygen -t ed25519 -C "your_email@example.com"
```

> 💡 **팁**: 
> - Enter를 누르면 기본 위치(`~/.ssh/id_ed25519` 또는 `~/.ssh/id_rsa`)에 저장됩니다
> - 비밀번호를 설정할지 물어보면, 설정하거나 Enter로 건너뛸 수 있습니다
> - 비밀번호를 설정하면 보안이 강화되지만, 매번 입력해야 합니다

---

## 2단계: SSH 공개 키 복사

> ⏭️ **GUI 사용자 (GitHub Desktop)**: 이 단계를 **건너뛰세요**. GitHub Desktop이 자동으로 처리합니다. → [4단계로 이동](#4단계-ssh-연결-테스트)

### 📋 공개 키 복사하기

터미널 명령어를 사용한 경우에만 이 단계를 진행하세요:

터미널에서 다음 명령어를 실행하여 공개 키를 클립보드에 복사합니다:

#### macOS
```bash
# Ed25519 키인 경우
cat ~/.ssh/id_ed25519.pub | pbcopy

# RSA 키인 경우
cat ~/.ssh/id_rsa.pub | pbcopy
```

#### Linux
```bash
# Ed25519 키인 경우
cat ~/.ssh/id_ed25519.pub | xclip -selection clipboard

# 또는
cat ~/.ssh/id_ed25519.pub
# 출력된 내용을 수동으로 복사
```

#### Windows (Git Bash)
```bash
cat ~/.ssh/id_ed25519.pub
# 출력된 내용을 수동으로 복사
```

#### Windows (PowerShell)
```powershell
Get-Content ~/.ssh/id_ed25519.pub | Set-Clipboard
```

> ⚠️ **중요**: 공개 키는 `ssh-rsa` 또는 `ssh-ed25519`로 시작하는 긴 문자열입니다. 전체를 복사해야 합니다.

---

## 3단계: GitHub에 SSH 키 추가

> ⏭️ **GUI 사용자 (GitHub Desktop)**: 이 단계를 **건너뛰세요**. GitHub Desktop이 자동으로 처리합니다. → [4단계로 이동](#4단계-ssh-연결-테스트)

### 📝 단계별 가이드

터미널 명령어를 사용한 경우에만 이 단계를 진행하세요:

1. **GitHub에 로그인**
   - [github.com](https://github.com) 접속 후 로그인

2. **Settings 페이지로 이동**
   - 우측 상단 프로필 아이콘 클릭
   - **Settings** 클릭

3. **SSH and GPG keys 메뉴 선택**
   - 왼쪽 사이드바에서 **SSH and GPG keys** 클릭

4. **새 SSH 키 추가**
   - **New SSH key** 버튼 클릭

5. **키 정보 입력**
   - **Title**: 키 이름 입력 (예: "MacBook Pro", "Windows PC", "회사 노트북")
   - **Key**: 2단계에서 복사한 공개 키 붙여넣기
   - **Key type**: 자동으로 감지됨 (보통 "Authentication Key")

6. **저장**
   - **Add SSH key** 클릭
   - 비밀번호 확인 요청 시 GitHub 비밀번호 입력

> ✅ **완료**: SSH 키가 GitHub에 추가되었습니다!

---

## 4단계: SSH 연결 테스트

### 🧪 연결 테스트

> 💡 **권장**: GUI 사용자도 이 단계를 진행하여 SSH 키가 제대로 설정되었는지 확인하는 것이 좋습니다.

터미널에서 다음 명령어를 실행하여 GitHub 연결을 테스트합니다:

```bash
ssh -T git@github.com
```

**처음 연결할 때:**
```
The authenticity of host 'github.com (IP)' can't be established.
Are you sure you want to continue connecting (yes/no/[fingerprint])?
```
→ `yes` 입력

**성공 메시지:**
```
Hi username! You've successfully authenticated, but GitHub does not provide shell access.
```

> ✅ 이 메시지가 보이면 SSH 키 설정이 완료된 것입니다!

---

## 5단계: Unity 프로젝트에서 패키지 설치

### 📦 패키지 설치 방법

#### 방법 1: 자동 설치 (권장)

1. **manifest.json 확인**
   - 프로젝트의 `Packages/manifest.json` 파일에 다음이 포함되어 있는지 확인:
   
   ```json
   {
     "dependencies": {
       "com.ninemood.services": "ssh://git@github.com/kwrain/NineMood.Services.git",
       ...
     }
   }
   ```

2. **Unity 에디터 실행**
   - Unity 에디터를 열거나 재시작
   - Unity가 자동으로 `manifest.json`을 읽고 패키지를 설치합니다

3. **설치 확인**
   - **Window** → **Package Manager** 열기
   - 왼쪽 상단에서 **Packages: In Project** 선택
   - `com.ninemood.services` 패키지가 목록에 표시되는지 확인

#### 방법 2: 수동 설치

패키지가 자동으로 설치되지 않으면:

1. **Window** → **Package Manager** 열기
2. 왼쪽 상단의 **+** 버튼 클릭
3. **Add package from git URL...** 선택
4. URL 입력: `ssh://git@github.com/kwrain/NineMood.Services.git`
5. **Add** 클릭

> 💡 **팁**: Unity를 완전히 종료 후 다시 시작하면 자동으로 설치됩니다.

---

## 문제 해결

### ❌ SSH 연결이 안 될 때

#### 1. SSH 에이전트 확인 및 키 추가

```bash
# SSH 에이전트 시작
eval "$(ssh-agent -s)"

# SSH 키 추가
ssh-add ~/.ssh/id_ed25519  # 또는 id_rsa
```

#### 2. GitHub 호스트 키 확인

```bash
ssh-keyscan github.com >> ~/.ssh/known_hosts
```

#### 3. SSH 설정 파일 확인

`~/.ssh/config` 파일을 생성하거나 수정:

```
Host github.com
  HostName github.com
  User git
  IdentityFile ~/.ssh/id_ed25519  # 또는 id_rsa
```

#### 4. SSH 키 권한 확인

```bash
# 개인 키 권한 확인 (600이어야 함)
chmod 600 ~/.ssh/id_ed25519
chmod 600 ~/.ssh/id_rsa

# 공개 키 권한 확인 (644이어야 함)
chmod 644 ~/.ssh/id_ed25519.pub
chmod 644 ~/.ssh/id_rsa.pub

# .ssh 디렉토리 권한 확인 (700이어야 함)
chmod 700 ~/.ssh
```

### ❌ Unity에서 패키지 설치 실패

#### 1. Unity 콘솔 확인
- **Window** → **General** → **Console** 열기
- 에러 메시지 확인

#### 2. Unity 재시작
- Unity 완전히 종료 후 다시 시작

#### 3. 패키지 캐시 삭제
- `Library/PackageCache` 폴더 삭제 (Unity 종료 후)
- Unity 재시작

#### 4. Git 버전 확인
- Unity는 Git 2.14.0 이상이 필요합니다
- 터미널에서 확인: `git --version`

### 🪟 Windows 사용자 특별 가이드

#### 사용 가능한 도구
- **Git Bash** (Git for Windows와 함께 설치됨) - 권장
- **Windows Terminal**
- **PowerShell**
- **PuTTY** (별도 SSH 키 생성 필요)

#### Windows에서 SSH 키 생성

**Git Bash:**
```bash
ssh-keygen -t ed25519 -C "your_email@example.com"
```

**PowerShell:**
```powershell
ssh-keygen -t ed25519 -C "your_email@example.com"
```

#### Windows에서 공개 키 확인

**Git Bash:**
```bash
cat ~/.ssh/id_ed25519.pub
```

**PowerShell:**
```powershell
Get-Content ~/.ssh/id_ed25519.pub
```

---

## 자주 묻는 질문

### Q1. GitHub 웹사이트에서 SSH 키를 생성할 수 있나요?

**A:** ❌ **아니요, 불가능합니다.** SSH 키는 보안상의 이유로 반드시 로컬 컴퓨터에서 생성해야 합니다. GitHub 웹사이트에서는 SSH 키를 생성할 수 없으며, 오직 공개 키를 추가하는 것만 가능합니다.

**대안:**
- GUI 도구 사용: GitHub Desktop, SourceTree, PuTTYgen 등
- 터미널 명령어 사용: `ssh-keygen` 명령어

### Q2. SSH 키는 각 팀원마다 만들어야 하나요?

**A:** 네, 각 팀원이 자신의 컴퓨터에서 SSH 키를 각각 만들어야 합니다. SSH 키는 개인 컴퓨터에 생성되며, 각자의 GitHub 계정에 추가해야 합니다.

### Q3. 여러 컴퓨터를 사용하는 경우?

**A:** 각 컴퓨터마다 SSH 키를 생성하고 GitHub에 추가해야 합니다. 한 컴퓨터의 SSH 키를 다른 컴퓨터에 복사하는 것은 보안상 권장하지 않습니다.

### Q4. SSH 키를 공유해도 되나요?

**A:** ❌ **절대 안 됩니다!**
- 개인 키(`id_ed25519`, `id_rsa`)는 절대 공유하지 마세요
- 공개 키(`.pub` 파일)만 GitHub에 추가합니다
- SSH 키는 비밀번호처럼 보안이 중요합니다

### Q5. SSH 키를 잊어버렸거나 분실했을 때?

**A:** 
1. GitHub에서 기존 SSH 키 삭제
2. 새로운 SSH 키 생성
3. 새 공개 키를 GitHub에 추가

### Q6. 패키지가 설치되지 않을 때?

**A:**
1. Unity 콘솔에서 에러 메시지 확인
2. SSH 연결 테스트: `ssh -T git@github.com`
3. Unity 완전히 종료 후 재시작
4. `Library/PackageCache` 폴더 삭제 후 Unity 재시작

### Q7. Personal Access Token과 SSH 키의 차이?

**A:**
- **SSH 키**: 한 번 설정하면 계속 사용 가능, 더 안전함 (권장)
- **Personal Access Token**: URL에 포함되어야 함, 보안상 덜 안전함

---

## 📚 참고 자료

- [GitHub SSH 키 가이드](https://docs.github.com/en/authentication/connecting-to-github-with-ssh)
- [Unity Git 패키지 가이드](https://docs.unity3d.com/Manual/upm-git.html)

---

## ✅ 체크리스트

설정 완료 후 확인사항:

- [ ] SSH 키 생성 완료
- [ ] 공개 키를 GitHub에 추가 완료
- [ ] `ssh -T git@github.com` 테스트 성공
- [ ] `manifest.json`에 패키지 URL 추가 완료
- [ ] Unity에서 패키지 설치 확인 완료

---

> 💬 **문제가 발생하면 팀 채널에 문의하세요!**
