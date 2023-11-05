<?php
class DataBase {

    private $DB_HOST = "localhost";
    private $DB_NAME = "";
    private $DB_USER = "";
    private $DB_PASS = "";
    private $PDO;

    public function __construct() {
        $this->PDO = new PDO("mysql:host=".$this->DB_HOST."; dbname=".$this->DB_NAME, $this->DB_USER, $this->DB_PASS, array(PDO::MYSQL_ATTR_INIT_COMMAND => "SET NAMES utf8"));
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
