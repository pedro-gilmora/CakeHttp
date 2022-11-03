package="$(find . -wholename '**/Release/*.nupkg' -exec echo "{}" \;)";
BRANCH_NAME=dev

if [ $BRANCH_NAME = 'dev' ]; then
    newPackageName="$(echo $package | sed 's/CakeHttp\./CakeHttp\.Dev\./')" ; 
    mv $package $newPackageName ; 
    package=$newPackageName ;
fi