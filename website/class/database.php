<?php
class DataBase {
	
    private $PDO;

    public function __construct() {
        $this->PDO = new PDO("mysql:host=".DB_HOST."; port=".DB_PORT."; dbname=".DB_NAME, DB_USER, DB_PASS, array(PDO::MYSQL_ATTR_INIT_COMMAND => "SET NAMES utf8"));
    }
    public function select($query, $bindings = []) {
        $STH = $this->PDO->prepare($query);
        $STH->execute($bindings);
        $result = $STH->fetchAll(PDO::FETCH_ASSOC);
		$result ??= false;
		return $result;
    }

    public function query($query, $bindings = []){
        $STH = $this->PDO->prepare($query);
        return $STH->execute($bindings);
    }

}
