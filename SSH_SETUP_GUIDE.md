# SSH 키 설정 가이드 (팀원용)

Unity 프로젝트에서 비공개 GitHub 저장소 패키지를 사용하기 위해 SSH 키를 설정하는 방법입니다.

## 🚀 빠른 시작 가이드

### GUI 도구 사용자 (GitHub Desktop) - 간단한 방법

1. **GitHub Desktop 설치 및 로그인** → SSH 키 자동 생성 및 추가 완료
2. **SSH 연결 테스트** (선택사항) → [4단계](#4-ssh-연결-테스트)
3. **Unity에서 패키지 설치** → [5단계](#5-unity-프로젝트에서-패키지-설치)

> ⏭️ **2단계와 3단계는 건너뛰어도 됩니다!** GitHub Desktop이 자동으로 처리합니다.

### 터미널 사용자 - 전체 단계 진행

1. SSH 키 확인 및 생성
2. SSH 공개 키 복사
3. GitHub에 SSH 키 추가
4. SSH 연결 테스트
5. Unity에서 패키지 설치

---

## 1. SSH 키 확인

터미널에서 다음 명령어를 실행하여 이미 SSH 키가 있는지 확인합니다:

```bash
ls -la ~/.ssh/id_*.pub
```

파일이 있으면 **2단계**로 넘어가세요. 없으면 **1-1단계**를 진행하세요.

### 1-1. SSH 키 생성 (키가 없는 경우)

> ⚠️ **중요**: SSH 키는 **반드시 로컬 컴퓨터에서 생성**해야 합니다. GitHub 웹사이트에서는 SSH 키를 생성할 수 없습니다.

#### 방법 1: GUI 도구 사용 (초보자 권장)

터미널 명령어가 어렵다면 GUI 도구를 사용할 수 있습니다:

**GitHub Desktop:**
1. [desktop.github.com](https://desktop.github.com)에서 다운로드 및 설치
2. GitHub 계정 로그인 시 **자동으로 SSH 키 생성 및 GitHub에 추가**
   - ✅ SSH 키 생성: 자동 처리
   - ✅ 공개 키 복사: 불필요 (자동 처리)
   - ✅ GitHub에 키 추가: 자동 처리
   - → **2단계와 3단계를 건너뛰고 4단계로 이동하세요**

#### 방법 2: 터미널 명령어 사용

**Ed25519 키 (권장):**
```bash
ssh-keygen -t ed25519 -C "your_email@example.com"
```

**RSA 키 (대안):**
```bash
ssh-keygen -t rsa -b 4096 -C "your_email@example.com"
```

- Enter를 누르면 기본 위치(`~/.ssh/id_ed25519` 또는 `~/.ssh/id_rsa`)에 저장됩니다
- 비밀번호를 설정할지 물어보면, 설정하거나 Enter로 건너뛸 수 있습니다

## 2. SSH 공개 키 복사

> ⏭️ **GUI 사용자 (GitHub Desktop)**: 이 단계를 **건너뛰세요**. GitHub Desktop이 자동으로 처리합니다. → [4단계로 이동](#4-ssh-연결-테스트)

터미널 명령어를 사용한 경우에만 이 단계를 진행하세요:

터미널에서 다음 명령어를 실행하여 공개 키를 복사합니다:

**Ed25519 키인 경우:**
```bash
cat ~/.ssh/id_ed25519.pub | pbcopy
```

**RSA 키인 경우:**
```bash
cat ~/.ssh/id_rsa.pub | pbcopy
```

또는 파일을 열어서 내용을 복사해도 됩니다.

## 3. GitHub에 SSH 키 추가

> ⏭️ **GUI 사용자 (GitHub Desktop)**: 이 단계를 **건너뛰세요**. GitHub Desktop이 자동으로 처리합니다. → [4단계로 이동](#4-ssh-연결-테스트)

터미널 명령어를 사용한 경우에만 이 단계를 진행하세요:

1. GitHub에 로그인합니다
2. 우측 상단 프로필 아이콘 클릭 → **Settings**
3. 왼쪽 사이드바에서 **SSH and GPG keys** 클릭
4. **New SSH key** 버튼 클릭
5. **Title**에 키 이름 입력 (예: "MacBook Pro", "Windows PC")
6. **Key** 필드에 복사한 공개 키 붙여넣기
7. **Add SSH key** 클릭

## 4. SSH 연결 테스트

> 💡 **권장**: GUI 사용자도 이 단계를 진행하여 SSH 키가 제대로 설정되었는지 확인하는 것이 좋습니다.

터미널에서 다음 명령어를 실행하여 GitHub 연결을 테스트합니다:

```bash
ssh -T git@github.com
```

처음 연결할 때 "Are you sure you want to continue connecting?" 메시지가 나오면 `yes`를 입력합니다.

성공하면 다음과 같은 메시지가 표시됩니다:
```
Hi username! You've successfully authenticated, but GitHub does not provide shell access.
```

## 5. Unity 프로젝트에서 패키지 설치

SSH 키 설정이 완료되면 다음 단계를 진행합니다:

### 5-1. manifest.json에 패키지 추가 (이미 추가되어 있다면 건너뛰기)

프로젝트의 `Packages/manifest.json` 파일을 열고, `dependencies` 섹션에 다음을 추가합니다:

```json
{
  "dependencies": {
    "com.ninemood.services": "ssh://git@github.com/kwrain/NineMood.Services.git",
    ...
  }
}
```

### 5-2. Unity에서 패키지 자동 설치

1. **Unity 에디터를 열거나 재시작**합니다
2. Unity가 자동으로 `manifest.json`을 읽고 패키지를 설치합니다
3. 설치가 완료되면 **Window** → **Package Manager**에서 확인할 수 있습니다
4. 왼쪽 상단에서 **Packages: In Project**를 선택하면 설치된 패키지 목록을 볼 수 있습니다

### 5-3. 수동으로 패키지 새로고침 (필요한 경우)

패키지가 자동으로 설치되지 않으면:

1. **Window** → **Package Manager** 열기
2. 왼쪽 상단의 **+** 버튼 클릭 → **Add package from git URL...**
3. URL 입력: `ssh://git@github.com/kwrain/NineMood.Services.git`
4. **Add** 클릭

또는 Unity를 재시작하면 자동으로 설치됩니다.

## 문제 해결

### SSH 연결이 안 될 때

1. **SSH 에이전트 확인:**
   ```bash
   eval "$(ssh-agent -s)"
   ssh-add ~/.ssh/id_ed25519  # 또는 id_rsa
   ```

2. **GitHub 호스트 키 확인:**
   ```bash
   ssh-keyscan github.com >> ~/.ssh/known_hosts
   ```

3. **SSH 설정 파일 확인 (`~/.ssh/config`):**
   ```
   Host github.com
     HostName github.com
     User git
     IdentityFile ~/.ssh/id_ed25519  # 또는 id_rsa
   ```

### Windows 사용자

Windows에서는 다음 도구를 사용할 수 있습니다:

1. **Git Bash** (Git for Windows와 함께 설치됨)
2. **Windows Terminal** 또는 **PowerShell**
3. **PuTTY** (별도 SSH 키 생성 필요)

Windows에서 SSH 키 생성:
```bash
ssh-keygen -t ed25519 -C "your_email@example.com"
```

공개 키 확인:
```bash
cat ~/.ssh/id_ed25519.pub
```

## 참고사항

- SSH 키는 한 번만 설정하면 계속 사용할 수 있습니다
- 여러 컴퓨터를 사용하는 경우 각 컴퓨터마다 SSH 키를 생성하고 GitHub에 추가해야 합니다
- SSH 키는 비밀번호처럼 보안이 중요하므로 절대 공유하지 마세요
- 공개 키(`.pub` 파일)만 GitHub에 추가하고, 개인 키(`id_ed25519` 또는 `id_rsa`)는 절대 공유하지 마세요
