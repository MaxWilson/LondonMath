pushd ~%dp0
git worktree add ..\LondonMathPublish gh-pages
git checkout master && npm run build && (robocopy /s deploy ..\LondonMathPublish || echo OK) && pushd ..\LondonMathPublish && git add . && git commit -m "New Version" && git push && popd
